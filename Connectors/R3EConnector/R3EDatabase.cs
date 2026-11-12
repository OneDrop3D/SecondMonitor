namespace SecondMonitor.R3EConnector
{
    using System.Collections.Generic;
    using System.IO;

    using Contracts;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class R3EDatabase
    {
        private readonly CachingDictionary<int, R3ECar> _r3ECars;
        private readonly CachingDictionary<int, string> _classNames;

        public R3EDatabase()
        {
            _r3ECars = new CachingDictionary<int, R3ECar>();
            _classNames = new CachingDictionary<int, string>();
        }

        private static string AssemblyDirectory
        {
            get
            {
                string path = System.AppDomain.CurrentDomain.BaseDirectory;
                return Path.GetDirectoryName(path);
            }
        }

        public void Load()
        {
            Load(Path.Combine(Path.Combine(AssemblyDirectory, @"Connectors\R3E\"), "r3e-data.json"));
        }

        public void Load(string filePath)
        {
            JObject jsonDb = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(filePath));
            JToken cars = jsonDb.GetValue("cars");
            JToken classes = jsonDb.GetValue("classes");
            LoadCarNames(cars);
            LoadClasses(classes);
        }

        private void LoadCarNames(JToken carsJson)
        {
            _r3ECars.Clear();
            foreach (var carJson in carsJson)
            {
                var carDefinition = carJson.First;
                int id = carDefinition.Value<int>("Id");
                string name = carDefinition.Value<string>("Name");
                JArray liveries = carDefinition.Value<JArray>("liveries");
                R3ECar newCar = new R3ECar(name, liveries);

                _r3ECars[id] = newCar;
            }
        }

        private void LoadClasses(JToken classesJson)
        {
            _classNames.Clear();
            foreach (var carJson in classesJson)
            {
                JToken classDefinition = carJson.First;
                int id = classDefinition.Value<int>("Id");
                string name = classDefinition.Value<string>("Name");
                _classNames[id] = name;
            }
        }

        public R3ECar GetCar(int id)
        {
            if (!_r3ECars.ContainsKey(id))
            {
                _r3ECars[id] = new R3ECar("Unknown");
            }

            return _r3ECars[id];
        }

        public string GetClassName(int id)
        {
            if (!_classNames.ContainsKey(id))
            {
                _classNames[id] = "Unknown";
            }

            return _classNames[id];
        }

        public IEnumerable<R3ECar> GetAllCars()
        {
            return _r3ECars.Values;
        }
    }
}