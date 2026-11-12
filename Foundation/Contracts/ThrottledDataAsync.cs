namespace SecondMonitor.Contracts
{
    using System;
    using System.Threading.Tasks;

    public class ThrottledDataAsync<T>
    {
        private readonly TimeSpan _retrievalTimeout;
        private readonly Func<T, Task<T>> _retrievalFunc;
        private DateTime _lastRetrieval = DateTime.MinValue;
        private T _lastValue = default(T);
        
        public ThrottledDataAsync(TimeSpan retrievalTimeout,  Func<T, Task<T>> retrievalFunc, T initialValue) : this(retrievalTimeout, retrievalFunc)
        {
            _lastValue = initialValue;
        }
        
        public ThrottledDataAsync(TimeSpan retrievalTimeout, Func<T, Task<T>> retrievalFunc)
        {
            _retrievalTimeout = retrievalTimeout;
            _retrievalFunc = retrievalFunc;
        }

        public async Task<T> GetValueAsync()
        {
            if (DateTime.UtcNow - _lastRetrieval > _retrievalTimeout)
            {
                _lastValue = await _retrievalFunc(_lastValue);
                _lastRetrieval = DateTime.UtcNow;
            }

            return _lastValue;
        }

        public void Reset(T initialValue)
        {
            _lastRetrieval = DateTime.UtcNow;
            _lastValue = initialValue;
        }
    }
}
