namespace SecondMonitor.Remote.Models
{
    using System;

    internal class NetStatSampler
    {
        private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private readonly int _sampleCapacity;
        private readonly long[] _bytesSamples;
        private readonly long[] _millisecondSamples;

        private int _head = 0;
        private int _tail = 0;
        private int _size = 0;

        public NetStatSampler(int sampleCapacity = 64)
        {
            _sampleCapacity = sampleCapacity;
            _bytesSamples = new long[sampleCapacity];
            _millisecondSamples = new long[sampleCapacity];
        }

        public long BytesPerSecond { get; private set; } = 0L;

        public string SpeedPerSecond
        {
            get
            {
                int decimalPlaces = 1;
                int i = 0;
                decimal dValue = BytesPerSecond;
                while (Math.Round(dValue, decimalPlaces) >= 1000)
                {
                    dValue /= 1024;
                    i++;
                }

                return string.Format("{0:n" + decimalPlaces + "} {1}/s", dValue, SizeSuffixes[i]);
            }
        }

        public void AddSample(long bytes, long elapsedMillis)
        {
            if (_size == _sampleCapacity)
            {
                _head = (_head + 1) % _sampleCapacity;
            }
            else
            {
                _size++;
            }

            _bytesSamples[_tail] = bytes;
            _millisecondSamples[_tail] = elapsedMillis;

            _tail = (_tail + 1) % _sampleCapacity;

            int index = _head;

            long bytesTotal = 0L;
            long millisecondsTotal = 0L;
            for (var i = 0; i <= _size; i++, index = (index + 1) % _sampleCapacity)
            {
                bytesTotal += _bytesSamples[index];
                millisecondsTotal += _millisecondSamples[index];
            }

            // Ensuring value is calculated as bytes per second instead of per millisecond
            if (millisecondsTotal == 0)
            {
                BytesPerSecond = 0;
            }
            else
            {
                BytesPerSecond = bytesTotal * 1000L / millisecondsTotal;
            }
        }
    }
}