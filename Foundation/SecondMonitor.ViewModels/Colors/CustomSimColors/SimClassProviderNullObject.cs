namespace SecondMonitor.ViewModels.Colors.CustomSimColors
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel.BasicProperties;

    public class SimClassProviderNullObject : ISimClassColorProvider
    {
        public string Simulator => string.Empty;

        public IReadOnlyDictionary<string, ColorDto> GetCustomClassColors()
        {
            return new Dictionary<string, ColorDto>();
        }
    }
}
