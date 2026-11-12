namespace SecondMonitor.ViewModels.Colors.CustomSimColors
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel.BasicProperties;

    public interface ISimClassColorProvider
    {
        public string Simulator { get; }

        public IReadOnlyDictionary<string, ColorDto> GetCustomClassColors();
    }
}
