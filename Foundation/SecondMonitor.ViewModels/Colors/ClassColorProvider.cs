namespace SecondMonitor.ViewModels.Colors
{
    using System.Collections.Generic;
    using DataModel.BasicProperties;

    public class ClassColorProvider : IClassColorProvider
    {
        private static readonly ColorDto AmColor = ColorDto.RedColor;
        private static readonly ColorDto ProColor = ColorDto.WhiteColor;
        private static readonly ColorDto ProAmColor = ColorDto.BlackColor;
        private static readonly ColorDto SilverColor = ColorDto.SilverColor;
        private static readonly ColorDto NationalColor = ColorDto.SilverColor;

        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly Dictionary<string, ColorDto> _cachedColors;

        public ClassColorProvider(IColorPaletteProvider colorPaletteProvider)
        {
            _colorPaletteProvider = colorPaletteProvider;
            _cachedColors = new Dictionary<string, ColorDto>();
        }

        public ColorDto GetColorForClass(string classId)
        {
            switch (classId)
            {
                case "ACC-Pro":
                    return ProColor;
                case "ACC-Pro-Am":
                    return ProAmColor;
                case "ACC-Am":
                    return AmColor;
                case "ACC-Silver":
                    return SilverColor;
                case "ACC-National":
                    return NationalColor;
                default:
                    return GetColorForClassDefault(classId);
            }
        }

        /*private ColorDto GetColorForAcc(string classId)
        {
            switch (classId)
            {
                case "Pro":
                    return ProColor;
                case "Pro-Am":
                    return ProAmColor;
                case "Am":
                    return AmColor;
                case "Silver":
                    return SilverColor;
                case "National":
                    return NationalColor;
                default:
                    return GetColorForClassDefault(classId);
            }
        }*/

        private ColorDto GetColorForClassDefault(string classId)
        {
            if (_cachedColors.TryGetValue(classId, out ColorDto classColor))
            {
                return classColor;
            }

            classColor = _colorPaletteProvider.GetNextAsDto();
            _cachedColors[classId] = classColor;
            return classColor;
        }

        public void Reset()
        {
            _cachedColors.Clear();
        }
    }
}