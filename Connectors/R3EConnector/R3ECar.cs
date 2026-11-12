namespace SecondMonitor.R3EConnector
{
    using Contracts;
    using Newtonsoft.Json.Linq;

    public class R3ECar
    {
        private readonly CachingDictionary<int, R3ELivery> _carLiveries;

        public R3ECar(string carName)
        {
            _carLiveries = new CachingDictionary<int, R3ELivery>();
            CarName = carName;
        }

        public R3ECar(string carName, JArray liveries) : this(carName)
        {
            foreach (JToken livery in liveries)
            {
                int liveryId = livery.Value<int>("Id");
                string teamName = livery.Value<string>("TeamName");
                int carNumber = int.TryParse(livery.Value<string>("Name").Replace("#", string.Empty), out int carNumberConverted) ? carNumberConverted : 0;
                _carLiveries.Add(liveryId, new R3ELivery(carNumber, teamName));
            }
        }

        public string CarName { get; }

        public bool TryGetLivery(int liveryNumber, out R3ELivery carLivery)
        {
            return _carLiveries.TryGetValue(liveryNumber, out carLivery);
        }
    }
}