namespace SecondMonitor.ViewModels.Layouts
{
    using System.Collections.Generic;
    using Settings.Model.Layout;

    public class MultipleColumnsLayout : AbstractViewModel
    {
        private ICollection<LengthDefinitionSetting> _columnsWidth;
        private ICollection<IViewModel> _viewModels;

        public MultipleColumnsLayout(ICollection<LengthDefinitionSetting> columnsWidth, ICollection<IViewModel> viewModels, bool addGridSplitters)
        {
            AddGridSplitters = addGridSplitters;
            _columnsWidth = columnsWidth;
            _viewModels = viewModels;
        }

        public bool AddGridSplitters { get; set; }

        public ICollection<IViewModel> ViewModels
        {
            get => _viewModels;
            set => SetProperty(ref _viewModels, value);
        }

        public ICollection<LengthDefinitionSetting> ColumnsWidth
        {
            get => _columnsWidth;
            set => SetProperty(ref _columnsWidth, value);
        }
    }
}