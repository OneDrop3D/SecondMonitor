namespace SecondMonitor.Contracts
{
    using System.Collections.Generic;
    using System.Linq;

    public abstract class AbstractHumanReadableMap<T>
    {
        protected AbstractHumanReadableMap()
        {
            Translations = new Dictionary<T, string>();
        }

        protected Dictionary<T, string> Translations { get; }

        public string ToHumanReadable(T value)
        {
            return Translations[value];
        }

        public T FromHumanReadable(string humanString)
        {
            return Translations.First(x => x.Value == humanString).Key;
        }

        public IEnumerable<string> GetAllHumanReadableValue()
        {
            return Translations.Values;
        }

        public IEnumerable<KeyValuePair<T, string>> GetAllTranslations()
        {
            return Translations.Select(x => new KeyValuePair<T, string>(x.Key, x.Value));
        }

        public IEnumerable<HumanReadableItem<T>> GetAllItems()
        {
            return Translations.Select(x => new HumanReadableItem<T>(x.Key, x.Value));
        }
    }
}