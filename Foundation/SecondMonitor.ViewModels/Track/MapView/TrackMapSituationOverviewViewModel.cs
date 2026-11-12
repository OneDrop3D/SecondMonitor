namespace SecondMonitor.ViewModels.Track.MapView
{
    using System.Collections.Generic;
    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.TrackMap;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Weather;

    public class TrackMapSituationOverviewViewModel : DefaultSituationOverviewViewModel
    {
        private bool _autoScaleDrivers;
        private double _driverScale;

        public TrackMapSituationOverviewViewModel(IViewModelFactory viewModelFactory, IClassColorProvider classColorProvider, double layoutLength, bool animateDrivers, int driversUpdatedPerTick) : base(viewModelFactory, classColorProvider, layoutLength, animateDrivers, driversUpdatedPerTick)
        {
            TrackWithSectorsGeometryViewModel = viewModelFactory.Create<TrackWithSectorsGeometryViewModel>();
            WindInformationViewModel = viewModelFactory.Create<WindInformationViewModel>();
            DriverScale = 0.08;
        }

        public bool AutoScaleDrivers
        {
            get => _autoScaleDrivers;
            set => SetProperty(ref _autoScaleDrivers, value);
        }

        public double DriverScale
        {
            get => _driverScale;
            set => SetProperty(ref _driverScale, value);
        }

        public TrackWithSectorsGeometryViewModel TrackWithSectorsGeometryViewModel { get; }
        public WindInformationViewModel WindInformationViewModel { get; }

        public override List<DriverInfo> Update(SimulatorDataSet simulatorDataSet, ISessionInformationProvider sessionInformationProvider, bool usePositionInClass)
        {
            WindInformationViewModel.FromModel(simulatorDataSet.SessionInfo.WeatherInfo);
            TrackWithSectorsGeometryViewModel.UpdateSectorStates(simulatorDataSet, sessionInformationProvider);
            return base.Update(simulatorDataSet, sessionInformationProvider, usePositionInClass);
        }

        public void ApplyTrackGeometry(TrackGeometryDto trackGeometryDto)
        {
            WindInformationViewModel.UpdateRotationByGeometryDto(trackGeometryDto);
            TrackWithSectorsGeometryViewModel.FromModel(trackGeometryDto);
        }
    }
}