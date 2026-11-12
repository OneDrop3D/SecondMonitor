namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using SessionEvents;
    using Settings;
    using Settings.ViewModel;
    using StatusIcon;

    public class DashboardViewModel : AbstractViewModel, ISimulatorDataSetViewModel
    {
        public const string ViewModelLayoutName = "Icons and Warnings Board";
        private readonly ISimSettingsFactory _settingsFactory;
        private readonly Stopwatch _refreshStopwatch;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private ISimSettings _currentSimSettings;
        private TimeSpan _lastTcActiveTime;
        private TimeSpan _lastAbsActiveTime;

        public DashboardViewModel(ISimSettingsFactory settingsFactory, ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider)
        {
            _settingsFactory = settingsFactory;
            _refreshStopwatch = Stopwatch.StartNew();
            EngineStatus = new DamageStatusIconViewModel();
            TransmissionStatus = new DamageStatusIconViewModel();
            SuspensionStatus = new DamageStatusIconViewModel();
            BodyworkStatus = new DamageStatusIconViewModel();
            BrakesStatus = new DamageStatusIconViewModel();
            ClutchStatus = new ClutchStatusIconViewModel();

            PitLimiterStatus = new StatusIconViewModel();
            AlternatorStatus = new StatusIconViewModel();
            TyreDirtStatus = new StatusIconViewModel();
            DrsStatusIndication = new StatusIconViewModel();
            BoostIndication = new StatusIconViewModel();
            HeadLightStatus = new StatusIconViewModel();
            AbsStatus = new StatusIconViewModel();
            TcStatus = new StatusIconViewModel();

            sessionEventProvider.SimulatorChanged += SessionEventProviderOnSimulatorChanged;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _currentSimSettings = _settingsFactory.Create(string.Empty);
        }

        public DamageStatusIconViewModel EngineStatus { get; }
        public DamageStatusIconViewModel TransmissionStatus { get; }
        public DamageStatusIconViewModel SuspensionStatus { get; }
        public DamageStatusIconViewModel BodyworkStatus { get; }
        public DamageStatusIconViewModel BrakesStatus { get; }
        public StatusIconViewModel PitLimiterStatus { get; }
        public StatusIconViewModel AlternatorStatus { get; }
        public StatusIconViewModel TyreDirtStatus { get; }
        public StatusIconViewModel DrsStatusIndication { get; }
        public StatusIconViewModel BoostIndication { get; }
        public ClutchStatusIconViewModel ClutchStatus { get; }
        public StatusIconViewModel HeadLightStatus { get; }
        public StatusIconViewModel AbsStatus { get; }
        public StatusIconViewModel TcStatus { get; }

        public void UpdateHighFrequencyInformation(SimulatorDataSet dataSet)
        {
            ApplyDateSet(dataSet);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (dataSet?.PlayerInfo?.CarInfo?.CarDamageInformation == null)
            {
                return;
            }

            UpdatePitLimiterStatus(dataSet);
            UpdateAlternatorStatus(dataSet);
            UpdateDrsStatus(dataSet.PlayerInfo.CarInfo);
            UpdateHeadlightStatus(dataSet.PlayerInfo.CarInfo);
            UpdateTcStatus(dataSet.PlayerInfo.CarInfo, dataSet.SessionInfo.SessionTime);
            UpdateAbsStatus(dataSet.PlayerInfo.CarInfo, dataSet.SessionInfo.SessionTime);

            if (_refreshStopwatch.ElapsedMilliseconds < 200)
            {
                return;
            }

            CarDamageInformation carDamage = dataSet.PlayerInfo?.CarInfo?.CarDamageInformation;
            if (carDamage == null)
            {
                return;
            }

            ShowHideDamageIcons();
            BodyworkStatus.ApplyDamage(carDamage.Bodywork);
            EngineStatus.ApplyDamage(carDamage.Engine);
            SuspensionStatus.ApplyDamage(carDamage.Suspension);
            TransmissionStatus.ApplyDamage(carDamage.Transmission);
            ApplyClutchDamage(carDamage.Clutch);
            ApplyBrakesDamage(dataSet);
            UpdateDirtLevel(dataSet.PlayerInfo.CarInfo);
            UpdateBoostStatus(dataSet.PlayerInfo.CarInfo.BoostSystem);

            if (dataSet.PlayerInfo.CarInfo.WheelsInfo.AllWheels.Any(x => x.Detached))
            {
                SuspensionStatus.IconState = StatusIconState.Error;
            }

            _refreshStopwatch.Restart();
        }

        private void UpdateTcStatus(CarInfo playerInfoCarInfo, TimeSpan sessionTime)
        {
            TcStatus.IsVisible = _displaySettingsViewModel.DashboardSettingsViewModel.TcEnabled && playerInfoCarInfo.TcInfo.IsAvailable;
            TcStatus.AdditionalText = playerInfoCarInfo.TcInfo.Remark;
            if (playerInfoCarInfo.TcInfo.IsActive)
            {
                TcStatus.IconState = StatusIconState.Warning;
                _lastTcActiveTime = sessionTime;
                return;
            }

            TcStatus.IconState = (sessionTime - _lastTcActiveTime).TotalSeconds < _displaySettingsViewModel.DashboardSettingsViewModel.TcAndAbsTimeout ? StatusIconState.Information : StatusIconState.Unlit;
        }

        private void UpdateAbsStatus(CarInfo playerInfoCarInfo, TimeSpan sessionTime)
        {
            AbsStatus.IsVisible = _displaySettingsViewModel.DashboardSettingsViewModel.AbsEnabled && playerInfoCarInfo.AbsInfo.IsAvailable;
            AbsStatus.AdditionalText = playerInfoCarInfo.AbsInfo.Remark;
            if (playerInfoCarInfo.AbsInfo.IsActive)
            {
                AbsStatus.IconState = StatusIconState.Warning;
                _lastAbsActiveTime = sessionTime;
                return;
            }

            AbsStatus.IconState = (sessionTime - _lastAbsActiveTime).TotalSeconds < _displaySettingsViewModel.DashboardSettingsViewModel.TcAndAbsTimeout ? StatusIconState.Information : StatusIconState.Unlit;
        }

        private void ShowHideDamageIcons()
        {
            BodyworkStatus.IsVisible = _currentSimSettings.IsBodyworkDamageProvided && _displaySettingsViewModel.DashboardSettingsViewModel.BodyWorkDamageEnabled;
            EngineStatus.IsVisible = _currentSimSettings.IsEngineDamageProvided && _displaySettingsViewModel.DashboardSettingsViewModel.EngineDamageEnabled;
            SuspensionStatus.IsVisible = _currentSimSettings.IsSuspensionDamageProvided && _displaySettingsViewModel.DashboardSettingsViewModel.SuspensionDamageEnabled;
            TransmissionStatus.IsVisible = _currentSimSettings.IsTransmissionDamageProvided && _displaySettingsViewModel.DashboardSettingsViewModel.TransmissionDamageEnabled;
            ClutchStatus.IsVisible = _currentSimSettings.IsClutchDamageProvided && _displaySettingsViewModel.DashboardSettingsViewModel.ClutchDamageEnabled;
            BrakesStatus.IsVisible = _currentSimSettings.IsBrakesDamageProvided && _displaySettingsViewModel.DashboardSettingsViewModel.BrakeDamageEnabled;
            TyreDirtStatus.IsVisible = _currentSimSettings.IsTyresDirtProvided && _displaySettingsViewModel.DashboardSettingsViewModel.TyreDirtEnabled;
        }

        private void UpdateHeadlightStatus(CarInfo playerInfoCarInfo)
        {
            HeadLightStatus.IsVisible = _displaySettingsViewModel.DashboardSettingsViewModel.LightsEnabled && playerInfoCarInfo.HeadLightsStatus != HeadLightsStatus.Na;
            switch (playerInfoCarInfo.HeadLightsStatus)
            {
                case HeadLightsStatus.NormalBeams:
                    HeadLightStatus.IconState = StatusIconState.Ok;
                    break;
                case HeadLightsStatus.HighBeams:
                    HeadLightStatus.IconState = StatusIconState.Information;
                    break;
                case HeadLightsStatus.Off:
                    HeadLightStatus.IconState = StatusIconState.Unlit;
                    break;
                case HeadLightsStatus.Na:
                default:
                    break;
            }
        }

        private void UpdateBoostStatus(BoostSystem boostSystem)
        {
            BoostIndication.IsVisible = _displaySettingsViewModel.DashboardSettingsViewModel.ErsEnabled && boostSystem.BoostStatus != BoostStatus.UnAvailable;

            if (boostSystem.CooldownTimer == TimeSpan.Zero && boostSystem.TimeRemaining == TimeSpan.Zero)
            {
                BoostIndication.AdditionalText = boostSystem.ActivationsRemaining.ToString();
            }
            else
            {
                BoostIndication.AdditionalText = boostSystem.TimeRemaining == TimeSpan.Zero ? boostSystem.CooldownTimer.TotalSeconds.ToString("F0") : boostSystem.TimeRemaining.TotalSeconds.ToString("F0");
            }

            BoostIndication.IconState = boostSystem.BoostStatus switch
            {
                BoostStatus.UnAvailable or BoostStatus.Off => StatusIconState.Unlit,
                BoostStatus.Available or BoostStatus.Build => StatusIconState.Information,
                BoostStatus.InUse or BoostStatus.Balanced => StatusIconState.Ok,
                BoostStatus.Cooldown or BoostStatus.Attack => StatusIconState.Warning,
                BoostStatus.Qualification => StatusIconState.Error,
                _ => BoostIndication.IconState
            };
        }

        private void UpdateDrsStatus(CarInfo playerInfoCarInfo)
        {
            DrsStatusIndication.IsVisible = _displaySettingsViewModel.DashboardSettingsViewModel.DrsEnabled && playerInfoCarInfo.DrsSystem.DrsStatus != DrsStatus.NotEquipped;

            DrsStatusIndication.AdditionalText = playerInfoCarInfo.DrsSystem.DrsActivationLeft < 0 ? string.Empty : playerInfoCarInfo.DrsSystem.DrsActivationLeft.ToString();
            switch (playerInfoCarInfo.DrsSystem.DrsStatus)
            {
                case DrsStatus.Available:
                    DrsStatusIndication.IconState = StatusIconState.Information;
                    break;
                case DrsStatus.InUse:
                    DrsStatusIndication.IconState = StatusIconState.Ok;
                    break;
                default:
                    DrsStatusIndication.IconState = StatusIconState.Unlit;
                    break;
            }
        }

        private void UpdateDirtLevel(CarInfo playerCar)
        {
            double maxDirt = playerCar.WheelsInfo.AllWheels.Max(x => x.DirtLevel);
            if (maxDirt < 0.01)
            {
                TyreDirtStatus.IconState = StatusIconState.Unlit;
                TyreDirtStatus.AdditionalText = string.Empty;
                return;
            }

            TyreDirtStatus.AdditionalText = ((int)(maxDirt * 100)).ToString();
            TyreDirtStatus.IconState = maxDirt > 0.5 ? StatusIconState.Error : StatusIconState.Warning;
        }

        private void UpdateAlternatorStatus(SimulatorDataSet dataSet)
        {
            if (!_displaySettingsViewModel.DashboardSettingsViewModel.AlternatorEnabled)
            {
                AlternatorStatus.IsVisible = false;
                return;
            }

            CarInfo playerInfoCarInfo = dataSet.PlayerInfo.CarInfo;
            AlternatorStatus.IconState = dataSet.SessionInfo.SessionType != SessionType.Na && playerInfoCarInfo.EngineRpm < 10 ? StatusIconState.Error : StatusIconState.Unlit;
            AlternatorStatus.IsVisible = AlternatorStatus.IconState != StatusIconState.Unlit;
        }

        public void Reset()
        {
            _lastAbsActiveTime = TimeSpan.Zero;
            _lastTcActiveTime = TimeSpan.Zero;
        }

        private void UpdatePitLimiterStatus(SimulatorDataSet dataSet)
        {
            if (!_displaySettingsViewModel.DashboardSettingsViewModel.PitLimiterEnabled)
            {
                PitLimiterStatus.IsVisible = false;
                return;
            }

            PitLimiterStatus.IsVisible = true;
            DriverInfo driver = dataSet.PlayerInfo;
            if (driver.InPits)
            {
                PitLimiterStatus.AdditionalText = string.Empty;
                PitLimiterStatus.IconState = driver.CarInfo.SpeedLimiterEngaged ? StatusIconState.Ok : StatusIconState.Warning;
                return;
            }

            if (driver.CarInfo.SpeedLimiterEngaged)
            {
                PitLimiterStatus.AdditionalText = string.Empty;
                PitLimiterStatus.IconState = StatusIconState.Error;
                return;
            }

            PitWindowInformation sessionInfoPitWindow = dataSet.SessionInfo.PitWindow;
            if (sessionInfoPitWindow.PitWindowState == PitWindowState.BeforePitWindow)
            {
                PitLimiterStatus.IconState = StatusIconState.Unlit;
                PitLimiterStatus.AdditionalText = dataSet.SessionInfo.SessionLengthType == SessionLengthType.Laps ? (sessionInfoPitWindow.PitWindowStart - driver.CompletedLaps) + "L" :
                    Math.Ceiling((TimeSpan.FromMinutes(dataSet.SessionInfo.PitWindow.PitWindowStart) - dataSet.SessionInfo.SessionTime).TotalMinutes).ToString("N0") + "M";
                return;
            }

            if (sessionInfoPitWindow.PitWindowState == PitWindowState.InPitWindow)
            {
                PitLimiterStatus.AdditionalText = dataSet.SessionInfo.SessionLengthType == SessionLengthType.Laps ? (sessionInfoPitWindow.PitWindowEnd - driver.CompletedLaps) + "L" :
                    Math.Ceiling((TimeSpan.FromMinutes(dataSet.SessionInfo.PitWindow.PitWindowEnd) - dataSet.SessionInfo.SessionTime).TotalMinutes).ToString("N0") + "M";
                PitLimiterStatus.IconState = StatusIconState.Information;
                return;
            }

            PitLimiterStatus.AdditionalText = string.Empty;
            PitLimiterStatus.IconState = StatusIconState.Unlit;
        }

        private void SessionEventProviderOnSimulatorChanged(object sender, DataSetArgs e)
        {
            _currentSimSettings = _settingsFactory.Create(e.DataSet.Source);
        }

        private void ApplyBrakesDamage(SimulatorDataSet dataSet)
        {
            Wheels wheels = dataSet.PlayerInfo.CarInfo.WheelsInfo;
            List<WheelInfo> brakesDamageOrdered = wheels.AllWheels.OrderByDescending(x => x.BrakesDamage.Damage).ToList();
            WheelInfo mostlyDamaged = brakesDamageOrdered.First();
            BrakesStatus.ApplyDamage(mostlyDamaged.BrakesDamage);

            WheelInfo frontDamage = wheels.FrontLeft.BrakesDamage.Damage > wheels.FrontRight.BrakesDamage.Damage ? wheels.FrontLeft : wheels.FrontRight;
            WheelInfo rearDamage = wheels.RearLeft.BrakesDamage.Damage > wheels.RearRight.BrakesDamage.Damage ? wheels.RearLeft : wheels.RearRight;

            double frontDamageValue = Math.Round(frontDamage.BrakesDamage.Damage * 100);
            double rearDamageValue = Math.Round(rearDamage.BrakesDamage.Damage * 100);

            BrakesStatus.AdditionalText = frontDamageValue > 0 || rearDamageValue > 0 ? $"{frontDamageValue:F0}/{rearDamageValue:F0}" : string.Empty;

            if (mostlyDamaged.BrakesDamage.Damage < mostlyDamaged.BrakesDamage.MediumDamageThreshold)
            {
                BrakesStatus.IconState = StatusIconState.Unlit;
                return;
            }

            if (mostlyDamaged.BrakesDamage.Damage < mostlyDamaged.BrakesDamage.HeavyDamageThreshold)
            {
                BrakesStatus.IconState = StatusIconState.Warning;
                return;
            }

            BrakesStatus.IconState = StatusIconState.Error;
        }

        private void ApplyClutchDamage(ClutchDamageInformation clutchDamageInformation)
        {
            ClutchStatus.ApplyDamage(clutchDamageInformation);

            if (!clutchDamageInformation.Temperature.IsZero)
            {
                ClutchStatus.Temperature = clutchDamageInformation.Temperature.GetFormattedWithUnits(0, _displaySettingsViewModel.TemperatureUnits);
            }
        }
    }
}