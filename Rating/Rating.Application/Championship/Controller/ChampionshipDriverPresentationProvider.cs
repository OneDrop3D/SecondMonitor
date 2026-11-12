namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using SecondMonitor.Contracts.SimSettings;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.DriversPresentation;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.SimdataManagement.DriverPresentation;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class ChampionshipDriverPresentationProvider : IAutomaticPresentationProvider, IChampionshipDriverPresentationProvider
    {
        private readonly ChampionshipSettingViewModel _championshipSetting;
        private readonly Dictionary<string, DriverPresentationDto> _driverPresentationMap;
        public event EventHandler<DriverCustomColorEnabledArgs> DriverCustomColorChanged;

        private ChampionshipDto _currentChampionship;

        public ChampionshipDriverPresentationProvider(ISettingsProvider settingsProvider)
        {
            _driverPresentationMap = new Dictionary<string, DriverPresentationDto>();
            _championshipSetting = settingsProvider.DisplaySettingsViewModel.ChampionshipSettingViewModel;
            _championshipSetting.PropertyChanged += ChampionshipSettingOnPropertyChanged;
        }

        public bool TryGetDriverPresentation(string driverLongName, out DriverPresentationDto driverPresentation)
        {
            return _driverPresentationMap.TryGetValue(driverLongName, out driverPresentation);
        }

        public void UpdateDriversOutLines(ChampionshipDto championshipDto)
        {
            _currentChampionship = championshipDto;
            UpdateDriversOutlinesInternal();
        }

        private void UpdateDriversOutlinesInternal()
        {
            if (_currentChampionship == null)
            {
                return;
            }

            ChampionshipDto championshipDto = _currentChampionship;
            HashSet<string> driversWithChangedOutlines = _driverPresentationMap.Keys.ToHashSet();
            _driverPresentationMap.Clear();

            driversWithChangedOutlines.ForEach(x => DriverCustomColorChanged?.Invoke(this, new DriverCustomColorEnabledArgs(x)));
            if (championshipDto.Drivers.All(x => x.TotalPoints == 0))
            {
                return;
            }

            AddDriverInRangeOutlines(championshipDto);
            AddLeaderOutline(championshipDto);

            _driverPresentationMap.Keys.ForEach(x => driversWithChangedOutlines.Add(x));
            driversWithChangedOutlines.ForEach(x => DriverCustomColorChanged?.Invoke(this, new DriverCustomColorEnabledArgs(x)));
        }

        private void AddDriverInRangeOutlines(ChampionshipDto championshipDto)
        {
            DriverDto player = championshipDto.Drivers.SingleOrDefault(x => x.IsPlayer);
            if (player == null)
            {
                return;
            }

            if (_championshipSetting.IsOutLineDriveLessPointsEnabled)
            {
                List<DriverDto> driverWithLessPoints = championshipDto.Drivers.Where(x => x.IsPlayer == false && x.TotalPoints <= player.TotalPoints && x.TotalPoints >= player.TotalPoints - _championshipSetting.MaxPointsDifferenceToOutline)
                    .OrderBy(x => x.Position).Take(_championshipSetting.MaxOutlinedDriveLessPoints).ToList();

                foreach (DriverDto x in driverWithLessPoints)
                {
                    double lambda = (player.TotalPoints - x.TotalPoints) / (double)(_championshipSetting.MaxPointsDifferenceToOutline);
                    ColorDto colorToUse = ColorDto.Interpolate(_championshipSetting.DriverLessPointsOutlineFrom, _championshipSetting.DriverLessPointsOutlineTo, lambda);
                    _driverPresentationMap[x.LastUsedName] = new DriverPresentationDto(x.LastUsedName, true, colorToUse);
                }
            }

            if (_championshipSetting.IsOutLineDriverMorePointsEnabled)
            {
                List<DriverDto> driverWithMorePoints = championshipDto.Drivers.Where(x => x.IsPlayer == false && x.TotalPoints >= player.TotalPoints && x.TotalPoints <= player.TotalPoints + _championshipSetting.MaxPointsDifferenceToOutline)
                    .OrderByDescending(x => x.Position).Take(_championshipSetting.MaxOutlinedDriversMorePoints).ToList();
                foreach (DriverDto x in driverWithMorePoints)
                {
                    double lambda = (x.TotalPoints - player.TotalPoints) / (double)(_championshipSetting.MaxPointsDifferenceToOutline);
                    ColorDto colorToUse = ColorDto.Interpolate(_championshipSetting.DriverMorePointsOutlineFrom, _championshipSetting.DriverMorePointsOutlineTo, lambda);
                    _driverPresentationMap[x.LastUsedName] = new DriverPresentationDto(x.LastUsedName, true, colorToUse);
                }
            }
        }

        private void AddLeaderOutline(ChampionshipDto championshipDto)
        {
            if (!_championshipSetting.IsOutlineLeaderEnabled)
            {
                return;
            }

            DriverDto leader = championshipDto.Drivers.SingleOrDefault(x => x.Position == 1);

            if (leader?.IsPlayer != false)
            {
                return;
            }

            _driverPresentationMap[leader.LastUsedName] = new DriverPresentationDto(leader.LastUsedName, true, _championshipSetting.LeaderOutLine);
        }

        private void ChampionshipSettingOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateDriversOutlinesInternal();
        }
    }
}
