namespace SecondMonitor.SimdataManagement.DriverPresentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.DriversPresentation;

    using SecondMonitor.DataModel.Extensions;

    public class DriverPresentationsManager : IDriverPresentationsManager
    {
        private readonly DriverPresentationsLoader _driverPresentationsLoader;
        private readonly IList<IAutomaticPresentationProvider> _automaticPresentationProviders;
        private readonly Lazy<Dictionary<string, DriverPresentationDto>> _driversPresentationMapLazyLoad;

        public DriverPresentationsManager(DriverPresentationsLoader driverPresentationsLoader, IList<IAutomaticPresentationProvider> automaticPresentationProviders)
        {
            _driverPresentationsLoader = driverPresentationsLoader;
            _automaticPresentationProviders = automaticPresentationProviders;
            _automaticPresentationProviders.ForEach(x => x.DriverCustomColorChanged += OnPresentationDriverCustomColorChanged);
            _driversPresentationMapLazyLoad = new Lazy<Dictionary<string, DriverPresentationDto>>(LoadDriversPresentationMap);
        }

        public event EventHandler<DriverCustomColorEnabledArgs> DriverCustomColorChanged;

        private Dictionary<string, DriverPresentationDto> DriversPresentationsMap => _driversPresentationMapLazyLoad.Value;

        public bool TryGetOutLineColor(string driverName, out ColorDto color)
        {
            bool containsDriver = TryGetDriverPresentation(driverName, out DriverPresentationDto driverPresentationDto);
            color = containsDriver ? driverPresentationDto.OutLineColor : null;
            return containsDriver;
        }

        public bool IsCustomOutlineEnabled(string driverName)
        {
            TryGetDriverPresentation(driverName, out DriverPresentationDto driverPresentationDto);
            return driverPresentationDto?.CustomOutLineEnabled ?? false;
        }

        public void SetOutLineColorEnabled(string driverName, bool isEnabled)
        {
            DriverPresentationDto driverPresentation = GetDriverOrCreatePresentation(driverName);
            driverPresentation.CustomOutLineEnabled = isEnabled;
            DriverCustomColorChanged?.Invoke(this, new DriverCustomColorEnabledArgs(driverName));
        }

        public void SetOutLineColor(string driverName, ColorDto color)
        {
            DriverPresentationDto driverPresentation = GetDriverOrCreatePresentation(driverName);
            driverPresentation.OutLineColor = color;
            DriverCustomColorChanged?.Invoke(this, new DriverCustomColorEnabledArgs(driverName));
        }

        public bool TryGetDriverPresentation(string driverLongName, out DriverPresentationDto driverPresentationDto)
        {
            if (DriversPresentationsMap.TryGetValue(driverLongName, out driverPresentationDto))
            {
                return true;
            }

            DriverPresentationDto automaticPresentationDto = null;
            IAutomaticPresentationProvider providerWithPresentation =
                _automaticPresentationProviders.FirstOrDefault(x => x.TryGetDriverPresentation(driverLongName, out automaticPresentationDto));
            driverPresentationDto = automaticPresentationDto;

            return providerWithPresentation != null;
        }

        public DriverPresentationDto GetDriverOrCreatePresentation(string driverName)
        {
            if (TryGetDriverPresentation(driverName, out DriverPresentationDto driverPresentationDto))
            {
                return driverPresentationDto;
            }

            driverPresentationDto = new DriverPresentationDto()
            {
                DriverName = driverName,
                CustomOutLineEnabled = false,
            };

            DriversPresentationsMap[driverName] = driverPresentationDto;
            return driverPresentationDto;
        }

        public void SavePresentations()
        {
            if (_driversPresentationMapLazyLoad.IsValueCreated)
            {
                _driverPresentationsLoader.Save(new DriverPresentationsDto() { DriverPresentations = DriversPresentationsMap.Values.ToList() });
            }
        }

        private DriverPresentationsDto LoadDriverPresentations()
        {
            return _driverPresentationsLoader.TryLoad(out DriverPresentationsDto driverPresentationsDto) ? driverPresentationsDto : new DriverPresentationsDto();
        }

        private Dictionary<string, DriverPresentationDto> LoadDriversPresentationMap()
        {
            return LoadDriverPresentations().DriverPresentations.Where(x => x.OutLineColor != null).ToDictionary(x => x.DriverName, x => x);
        }
        
        private void OnPresentationDriverCustomColorChanged(object sender, DriverCustomColorEnabledArgs e)
        {
            DriverCustomColorChanged?.Invoke(sender, e);
        }
    }
}