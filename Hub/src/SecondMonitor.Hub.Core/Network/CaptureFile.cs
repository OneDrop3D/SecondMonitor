namespace SecondMonitor.Hub.Core.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Enregistrement brut de ce que le collecteur envoie (paquets tels que reçus), pour rejouer une session et
    /// comprendre un problème après coup (« --rejouer fichier »). Écriture en arrière-plan ; taille plafonnée :
    /// 150 Mo par fichier, les 5 derniers fichiers sont gardés.
    /// Format : [int64 temps (ticks UTC)] [int32 longueur] [octets du paquet] répétés.
    /// </summary>
    public sealed class CaptureWriter : IDisposable
    {
        private const long MaxFileBytes = 150L * 1024 * 1024;
        private const int KeepFiles = 5;

        private readonly string _directory;
        private readonly BlockingCollection<(long Ticks, byte[] Data)> _queue = new(boundedCapacity: 5000);
        private readonly Thread _thread;
        private BinaryWriter _writer;
        private long _written;

        public CaptureWriter(string directory)
        {
            _directory = directory;
            _thread = new Thread(Run) { IsBackground = true, Name = "Enregistrement", Priority = ThreadPriority.BelowNormal };
            _thread.Start();
        }

        public string CurrentFile { get; private set; }

        /// <summary>Ajoute un paquet (copie). Ne bloque jamais la réception : en cas d'engorgement, le paquet est ignoré.</summary>
        public void Add(byte[] data, int offset, int count)
        {
            var copy = new byte[count];
            Buffer.BlockCopy(data, offset, copy, 0, count);
            _queue.TryAdd((DateTime.UtcNow.Ticks, copy));
        }

        /// <summary>Termine le fichier en cours (nouvelle connexion = nouveau fichier).</summary>
        public void Split() => _queue.TryAdd((0, null));

        public void Dispose()
        {
            _queue.CompleteAdding();
            _thread.Join(2000);
            _writer?.Dispose();
        }

        private void Run()
        {
            foreach (var (ticks, data) in _queue.GetConsumingEnumerable())
            {
                try
                {
                    if (data == null)
                    {
                        Close();
                        continue;
                    }

                    if (_writer == null || _written > MaxFileBytes)
                    {
                        Open();
                    }

                    _writer.Write(ticks);
                    _writer.Write(data.Length);
                    _writer.Write(data);
                    _written += 12 + data.Length;
                    if (_queue.Count == 0)
                    {
                        _writer.Flush();
                    }
                }
                catch (Exception)
                {
                    // Disque plein ou dossier en lecture seule : l'enregistrement s'arrête, la course continue.
                    Close();
                }
            }

            Close();
        }

        private void Open()
        {
            Close();
            Directory.CreateDirectory(_directory);
            CurrentFile = Path.Combine(_directory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.smrec");
            _writer = new BinaryWriter(new FileStream(CurrentFile, FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 16));
            _written = 0;
            foreach (string old in Directory.GetFiles(_directory, "*.smrec").OrderByDescending(f => f).Skip(KeepFiles))
            {
                try
                {
                    File.Delete(old);
                }
                catch (IOException)
                {
                }
            }
        }

        private void Close()
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public static class CaptureReader
    {
        /// <summary>Paquets d'un enregistrement, dans l'ordre, avec leur heure de réception.</summary>
        public static IEnumerable<(DateTime Time, byte[] Data)> Read(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            while (reader.BaseStream.Position + 12 <= reader.BaseStream.Length)
            {
                long ticks = reader.ReadInt64();
                int length = reader.ReadInt32();
                if (length < 0 || reader.BaseStream.Position + length > reader.BaseStream.Length)
                {
                    yield break; // fichier coupé (application fermée pendant l'écriture)
                }

                yield return (new DateTime(ticks, DateTimeKind.Utc), reader.ReadBytes(length));
            }
        }
    }
}
