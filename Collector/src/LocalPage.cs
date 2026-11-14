namespace SecondMonitor.Collector
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Sert la page de la fenêtre sur http://127.0.0.1:&lt;port&gt; (boucle locale uniquement, rien n'est ouvert sur le réseau).
    /// Une page chargée ainsi est « sécurisée » pour la vue web : elle peut utiliser le micro et choisir la sortie audio,
    /// ce qu'une page chargée directement en mémoire ne peut pas. Le port est fixe quand c'est possible : la vue web
    /// garde alors d'une fois sur l'autre les autorisations et les identifiants des appareils audio.
    /// </summary>
    internal sealed class LocalPage : IDisposable
    {
        /// <summary>Port préféré (TCP, sur la boucle locale seulement).</summary>
        public const int PreferredPort = 52643;

        private readonly TcpListener _listener;
        private readonly byte[] _page;

        private LocalPage(TcpListener listener, string html)
        {
            _listener = listener;
            _page = Encoding.UTF8.GetBytes(html);
            Url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
            _ = Task.Run(AcceptLoopAsync);
        }

        public string Url { get; }

        /// <summary>Démarre le petit serveur ; null s'il est impossible (la page est alors chargée en mémoire, sans micro).</summary>
        public static LocalPage TryStart(string html)
        {
            foreach (int port in new[] { PreferredPort, 0 })
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    return new LocalPage(listener, html);
                }
                catch (SocketException)
                {
                    // Port pris : on en prend un autre.
                }
            }

            return null;
        }

        public void Dispose()
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception)
            {
                // Déjà arrêté.
            }
        }

        private async Task AcceptLoopAsync()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return; // serveur arrêté
                }

                _ = Task.Run(() => ServeAsync(client));
            }
        }

        private async Task ServeAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = 5000;
                    NetworkStream stream = client.GetStream();
                    string request = await ReadHeadAsync(stream).ConfigureAwait(false);
                    string[] first = request.Split("\r\n")[0].Split(' ');
                    string path = first.Length >= 2 ? first[1].Split('?')[0] : string.Empty;
                    bool page = first[0] == "GET" && path is "/" or "/index.html";
                    byte[] body = page ? _page : Encoding.UTF8.GetBytes("Introuvable");
                    string head = page
                        ? "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\n"
                        : "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain; charset=utf-8\r\n";
                    head += $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(head)).ConfigureAwait(false);
                    await stream.WriteAsync(body).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Requête interrompue : sans importance.
                }
            }
        }

        private static async Task<string> ReadHeadAsync(Stream stream)
        {
            var buffer = new byte[4096];
            var text = new StringBuilder();
            while (text.Length < 16 * 1024)
            {
                var read = stream.ReadAsync(buffer).AsTask();
                if (await Task.WhenAny(read, Task.Delay(5000)).ConfigureAwait(false) != read || read.Result == 0)
                {
                    break;
                }

                text.Append(Encoding.ASCII.GetString(buffer, 0, read.Result));
                if (text.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                {
                    break;
                }
            }

            return text.ToString();
        }
    }
}
