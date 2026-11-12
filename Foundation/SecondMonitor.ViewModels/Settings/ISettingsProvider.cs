namespace SecondMonitor.ViewModels.Settings
{
    using System;
    using System.ComponentModel;
    using ViewModel;

    public interface ISettingsProvider
    {
        event EventHandler<PropertyChangedEventArgs> DisplaySettingsPropertyChanged;

        DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        string TelemetryRepositoryPath { get; }

        string MapRepositoryPath { get; }

        string RatingsRepositoryPath { get; }

        string SimulatorContentRepository { get; }

        string TrackRecordsPath { get; }

        string CarSpecificationPath { get; }

        string ChampionshipRepositoryPath { get; }

        string ChampionshipCalendarsPath { get; }

        string DriversPresentationFolder { get; }
    }
}