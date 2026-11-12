namespace SecondMonitor.Contracts
{
    using System;

    public class ThrottledData<T>
    {
        private readonly TimeSpan _retrievalTimeout;
        private readonly Func<T, T> _retrievalFunc;
        private DateTime _lastRetrieval = DateTime.MinValue;
        private T _lastValue = default(T);
        
        public ThrottledData(TimeSpan retrievalTimeout, Func<T, T> retrievalFunc, T initialValue) : this(retrievalTimeout, retrievalFunc)
        {
            _lastValue = initialValue;
        }
        
        public ThrottledData(TimeSpan retrievalTimeout, Func<T, T> retrievalFunc)
        {
            _retrievalTimeout = retrievalTimeout;
            _retrievalFunc = retrievalFunc;
        }

        public T GetValue()
        {
            if (DateTime.UtcNow - _lastRetrieval > _retrievalTimeout)
            {
                _lastValue = _retrievalFunc(_lastValue);
                _lastRetrieval = DateTime.UtcNow;
            }

            return _lastValue;
        }
    }
}
