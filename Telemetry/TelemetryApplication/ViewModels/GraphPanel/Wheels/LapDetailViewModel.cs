namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using SecondMonitor.ViewModels;

    public class LapDetailViewModel : AbstractViewModel
    {
        private string _lapName;
        private string _dataInfo;

        public LapDetailViewModel(string lapId)
        {
            LapId = lapId;
        }

        public string LapId
        {
            get;
        }

        public string LapName
        {
            get => _lapName;
            set => SetProperty(ref _lapName, value);
        }

        public string DataInfo
        {
            get => _dataInfo;
            set => SetProperty(ref _dataInfo, value);
        }
    }
}