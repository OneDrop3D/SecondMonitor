namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ManufacturersSessionResultViewModel : AbstractViewModel<IHaveManufacturersResults>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;
        private bool _hasAnyResults;

        public ManufacturersSessionResultViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            ManufacturerFinish = new List<ManufacturerFinishViewModel>();
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public bool HasAnyResults
        {
            get => _hasAnyResults;
            set => SetProperty(ref _hasAnyResults, value);
        }

        public List<ManufacturerFinishViewModel> ManufacturerFinish { get; private set; }

        protected override void ApplyModel(IHaveManufacturersResults model)
        {
            ManufacturerFinish.Clear();
            foreach (ManufacturerSessionResultDto manufacturerSessionResult in model.ManufacturersSessionResult.Where(x => !x.SkippedEvent).OrderBy(x => x.FinishPosition))
            {
                var newViewModel = _viewModelFactory.Create<ManufacturerFinishViewModel>();
                newViewModel.FromModel(manufacturerSessionResult);
                ManufacturerFinish.Add(newViewModel);
            }

            HasAnyResults = ManufacturerFinish.Count > 0;
        }

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}