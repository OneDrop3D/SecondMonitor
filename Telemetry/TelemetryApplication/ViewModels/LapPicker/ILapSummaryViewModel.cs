namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapPicker
{
    using System;
    using System.Windows.Input;
    using System.Windows.Media;
    using SecondMonitor.ViewModels;
    using TelemetryManagement.DTO;

    public interface ILapSummaryViewModel : IViewModel<LapSummaryDto>
    {
        int LapNumber { get; set; }
        string CustomName { get; }
        TimeSpan LapTime { get; }
        TimeSpan Sector1Time { get; }
        TimeSpan Sector2Time { get; }
        TimeSpan Sector3Time { get; }
        bool Selected { get; set; }
        Color LapColor { get; set; }
        SolidColorBrush LapColorBrush { get; }
        bool HasLapTime { get; }

        bool IsPlayerLap { get; set; }

        int Stint { get; set; }
        ICommand RemoveLapCommand { get; set; }
        ICommand ShowTelemetryChecklist { get; set; }

        bool IsVisible { get; set; }

        string DriverName { get; set; }

        bool IsDriverNameVisible { get; set; }

        void SetLapSelectedSilent(bool isSelected);

        bool IsValid { get; }
    }
}