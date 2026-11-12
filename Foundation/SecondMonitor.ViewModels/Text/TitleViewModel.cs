namespace SecondMonitor.ViewModels.Text
{
    public class TitleViewModel : AbstractViewModel<string>
    {
        private string _title;

        public TitleViewModel()
        {
        }

        public TitleViewModel(string title)
        {
            _title = title;
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        protected override void ApplyModel(string model)
        {
            Title = model;
        }

        public override string SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}