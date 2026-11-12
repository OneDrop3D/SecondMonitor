namespace SecondMonitor.ViewModels.Colors.CustomSimColors
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;

    public class LmUClassColors : ISimClassColorProvider
    {
        private static readonly IReadOnlyDictionary<string, ColorDto> customColors = new Dictionary<string, ColorDto>()
        {
            { SimulatorsNameMap.LmUHyperCarClassId, new ColorDto(255, 224, 12, 6) },
            { "LMP2", new ColorDto(255, 4, 77, 141) },
            { "LMP3", new ColorDto(255, 65, 28, 82) },
            { "LMP2_ELMS", new ColorDto(255, 4, 77, 141) },
            { "GTE", new ColorDto(255, 255, 139, 0) },
            { SimulatorsNameMap.LmUGT3CarClassId, new ColorDto(255, 0, 150, 57) }
        };

        public string Simulator => SimulatorsNameMap.LmUSimName;

        public IReadOnlyDictionary<string, ColorDto> GetCustomClassColors() => customColors;
    }
}
