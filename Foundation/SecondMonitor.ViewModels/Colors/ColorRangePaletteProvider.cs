namespace SecondMonitor.ViewModels.Colors
{
    using System.Collections.Generic;
    using DataModel.BasicProperties;
    using Extensions;

    public class ColorRangePaletteProvider : AbstractColorPaletteProvider
    {
        public ColorRangePaletteProvider(ColorDto fromColor, ColorDto toColor, int steps)
        {
            Colors = InitializeColorPalette(fromColor, toColor, steps);
        }

        protected override ColorDto[] Colors { get; }

        private ColorDto[] InitializeColorPalette(ColorDto fromColor, ColorDto toColor, int steps)
        {
            HslColor fromHslColor = HslColor.FromRGB(fromColor.Red, fromColor.Green, fromColor.Blue);
            HslColor toHslColor = HslColor.FromRGB(toColor.Red, toColor.Green, toColor.Blue);

            List<ColorDto> colors = new List<ColorDto>();
            double step = 1.0 / (steps + 1);

            for (int i = 0; i < steps + 1; i++)
            {
                HslColor currentColor = HslColor.Interpolate(fromHslColor, toHslColor, step * i);
                colors.Add(ColorDto.FromColor(currentColor.ToColor().ToMediaColor()));
            }

            colors.Add(toColor);
            return colors.ToArray();
        }
    }
}