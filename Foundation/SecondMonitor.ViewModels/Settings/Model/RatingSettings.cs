namespace SecondMonitor.ViewModels.Settings.Model
{
    public class RatingSettings
    {
        public bool IsEnabled { get; set; } = false;

        public string SelectedReferenceRatingProvider { get; set; } = "Leading Group";

        public int GraceLapsCount { get; set; } = 1;

        public int EloK { get; set; } = 15;

        public double AiCompetitiveRange { get; set; } = 2.2;

        public int NormalizedFieldSize { get; set; } = 20;
    }
}