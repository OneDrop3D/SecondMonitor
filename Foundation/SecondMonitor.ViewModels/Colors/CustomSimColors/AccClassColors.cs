namespace SecondMonitor.ViewModels.Colors.CustomSimColors
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;

    public class AccClassColors : ISimClassColorProvider
    {
        private static readonly IReadOnlyDictionary<string, ColorDto> customColors = new Dictionary<string, ColorDto>()
        {
            { "ACC-GT3", new ColorDto(255, 124, 124, 124) },
            { "ACC-GT2", new ColorDto(255, 255, 46, 0) },
            { "ACC-GT4", new ColorDto(255, 38, 38, 96) },
            { "ACC-Super Trofeo 2018", new ColorDto(255, 204, 186, 0) },
            { "ACC-Super Trofeo 2022", new ColorDto(255, 152, 138, 0) },
            { "ACC-Porsche GT3 Cup 2018", new ColorDto(255, 69, 124, 69) },
            { "ACC-Porsche GT3 Cup 2021", new ColorDto(255, 40, 76, 40) },
            { "ACC-Ferrari Challenge 2020", new ColorDto(255, 185, 0, 0) },
            { "ACC-TCX", new ColorDto(255, 0, 124, 167) }
        };

        public string Simulator => SimulatorsNameMap.AccSimName;

        public IReadOnlyDictionary<string, ColorDto> GetCustomClassColors() => customColors;
    }
}
