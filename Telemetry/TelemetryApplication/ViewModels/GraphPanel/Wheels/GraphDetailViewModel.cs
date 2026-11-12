namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using SecondMonitor.ViewModels;

    public class GraphDetailViewModel : AbstractViewModel
    {
        public GraphDetailViewModel()
        {
            LapDetails = new ObservableCollection<LapDetailViewModel>();
        }

        public ObservableCollection<LapDetailViewModel> LapDetails { get; }

        public LapDetailViewModel GetOrCreateLapDetail(string lapId)
        {
            var lapDetail = LapDetails.FirstOrDefault(x => x.LapId == lapId);
            if (lapDetail != null)
            {
                return lapDetail;
            }

            lapDetail = new LapDetailViewModel(lapId);
            LapDetails.Add(lapDetail);

            return lapDetail;
        }

        public void RemoveLapDetail(string lapId)
        {
            var lapDetail = LapDetails.FirstOrDefault(x => x.LapId == lapId);
            if (lapDetail != null)
            {
                LapDetails.Remove(lapDetail);
            }
        }
    }
}