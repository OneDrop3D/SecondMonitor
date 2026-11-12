namespace SecondMonitor.Rating.Application.Championship.Manufacturers
{
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("{Name}")]
    public class Manufacturer
    {
        private readonly string _manufacturerLowerCase;
        private readonly string[] _alternativeNames;
        public Manufacturer(string name, params string[] alternativeNames)
        {
            Name = name;
            _manufacturerLowerCase = Name.ToLower();
            _alternativeNames = alternativeNames;
        }

        public string Name { get; }

        public bool IsManufacturerOf(string carName)
        {
            string carNameLower = carName.ToLower();
            return carNameLower.Contains(_manufacturerLowerCase.ToLower())
                   || _alternativeNames.Any(x => carNameLower.Contains(x));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}