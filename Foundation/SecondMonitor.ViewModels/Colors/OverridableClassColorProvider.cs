namespace SecondMonitor.ViewModels.Colors
{
    using System.Collections.Generic;
    using System.Linq;

    using DataModel.BasicProperties;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Colors.CustomSimColors;
    using SecondMonitor.ViewModels.SessionEvents;

    public class OverridableClassColorProvider : IClassColorProvider
    {
        private static readonly ColorDto AmColor = ColorDto.RedColor;
        private static readonly ColorDto ProColor = ColorDto.WhiteColor;
        private static readonly ColorDto ProAmColor = ColorDto.BlackColor;
        private static readonly ColorDto SilverColor = ColorDto.SilverColor;
        private static readonly ColorDto NationalColor = ColorDto.SilverColor;

        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly IList<ISimClassColorProvider> _simClassColorProviders;
        private readonly Dictionary<string, ColorDto> _cachedColors;
        private ISimClassColorProvider _currentSimClassColorProvider;

        public OverridableClassColorProvider(IColorPaletteProvider colorPaletteProvider, ISessionEventProvider sessionEventProvider, IList<ISimClassColorProvider> simClassColorProviders)
        {
            sessionEventProvider.SimulatorChanged += SessionEventProviderOnSimulatorChanged;
            _colorPaletteProvider = colorPaletteProvider;
            _simClassColorProviders = simClassColorProviders;
            _cachedColors = new Dictionary<string, ColorDto>();

            if (sessionEventProvider.LastDataSet != null)
            {
                ReinitializeCustomClassColorProvider(sessionEventProvider.LastDataSet.Source);
            }
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

        private void SessionEventProviderOnSimulatorChanged(object sender, DataSetArgs e)
        {
            ReinitializeCustomClassColorProvider(e.DataSet.Source);
        }

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
            _currentSimClassColorProvider.GetCustomClassColors().ForEach(x => _cachedColors.Add(x.Key, x.Value));
        }

        private void ReinitializeCustomClassColorProvider(string simName)
        {
            _currentSimClassColorProvider = _simClassColorProviders.FirstOrDefault(x => x.Simulator == simName) ?? new SimClassProviderNullObject();
            Reset();
        }
    }
}