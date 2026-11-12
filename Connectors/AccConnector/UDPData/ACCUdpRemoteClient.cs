namespace SecondMonitor.AccConnector.UDPData
{
    using System;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using NLog;

    public class AccUdpRemoteClient : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly string _ip;
        private readonly int _port;

        private readonly Task _listenerTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _disposedValue = false; // To detect redundant calls
        private UdpClient _client;

        /// <summary>
        /// To get the events delivered inside the UI thread, just create this object from the UI thread/synchronization context.
        /// </summary>
        public AccUdpRemoteClient(string ip, int port, string displayName, string connectionPassword, string commandPassword, int msRealtimeUpdateInterval)
        {
            _ip = ip;
            _port = port;
            IpPort = $"{ip}:{port}";
            Log.Info($"Connecting to ACC with {IpPort}, name: {displayName}, password {connectionPassword}, command password {commandPassword}, updateInterval {msRealtimeUpdateInterval}");
            MessageHandler = new BroadcastingNetworkProtocol(IpPort, Send);
            _cancellationTokenSource = new CancellationTokenSource();
            DisplayName = displayName;
            ConnectionPassword = connectionPassword;
            CommandPassword = commandPassword;
            MsRealtimeUpdateInterval = msRealtimeUpdateInterval;

            _listenerTask = ConnectAndRun(_cancellationTokenSource.Token);
        }

        public BroadcastingNetworkProtocol MessageHandler { get; }
        public string IpPort { get; }
        public string DisplayName { get; }
        public string ConnectionPassword { get; }
        public string CommandPassword { get; }
        public int MsRealtimeUpdateInterval { get; }

        public bool IsConnected { get; private set; }

        private void Send(byte[] payload)
        {
            var sent = _client.Send(payload, payload.Length);
        }

        public void Shutdown()
        {
            ShutdownAsync().ContinueWith(t =>
             {
                 if (t.Exception?.InnerExceptions?.Any() == true)
                 {
                     Log.Error($"Client shut down with {t.Exception.InnerExceptions.Count} errors");
                 }
                 else
                 {
                     Log.Info("Client shut down asynchronously");
                 }
             });
        }

        public async Task ShutdownAsync()
        {
            if (_listenerTask?.IsCompleted == false)
            {
                IsConnected = false;
                MessageHandler.Disconnect();
                _cancellationTokenSource.Cancel();
                try
                {
                    await _listenerTask;
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        public void RefreshCarList()
        {
            MessageHandler.RequestEntryList();
        }

        private async Task ConnectAndRun(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _client = new UdpClient();
                    _client.Connect(_ip, _port);
                    await Task.Delay(100, cancellationToken);
                    bool inSession = true;
                    bool pingRequest = true;
                    MessageHandler.RequestConnection(DisplayName, ConnectionPassword, MsRealtimeUpdateInterval, CommandPassword);
                    while (inSession && !cancellationToken.IsCancellationRequested)
                    {
                        UdpReceiveResult updResult = await TryReceive(false);
                        if (updResult.Buffer.Length > 0)
                        {
                            pingRequest = false;
                        }
                        else
                        {
                            if (pingRequest)
                            {
                                Log.Info("ACC Broadcast server did not respond in proper time for ping request.");
                                inSession = false;
                                MessageHandler.Disconnect();
                            }
                            else
                            {
                                Log.Info("Did not receive any datagram from ACC for some period, sening ping request.");
                                pingRequest = true;
                                MessageHandler.RequestEntryList();
                            }

                            continue;
                        }

                        IsConnected = true;
                        using (var ms = new System.IO.MemoryStream(updResult.Buffer))
                        using (var reader = new System.IO.BinaryReader(ms))
                        {
                            MessageHandler.ProcessMessage(reader);
                        }
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    Log.Error(ex);
                    // Shutdown happened
                }
                catch (Exception ex)
                {
                    // Other exceptions
                    await Task.Delay(5000, cancellationToken);
                    Log.Error(ex);
                }
                finally
                {
                    _client.Close();
                    _client.Dispose();
                }
            }
        }

        private async Task<UdpReceiveResult> TryReceive(bool unlimitedTime)
        {
            Task<UdpReceiveResult> result = _client.ReceiveAsync();
            if (unlimitedTime)
            {
                return await result;
            }
            else
            {
                Task delayTask = Task.Delay(20000);
                await Task.WhenAny(result, delayTask);
                return result.IsCompleted ? result.Result : default(UdpReceiveResult);
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (_client != null)
                        {
                            _client.Close();
                            _client.Dispose();
                            _client = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ACCUdpRemoteClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
