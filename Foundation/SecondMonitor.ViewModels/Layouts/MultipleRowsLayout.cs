namespace SecondMonitor.ViewModels.Layouts
{
    using System.Collections.Generic;
    using Settings.Model.Layout;

    public class MultipleRowsLayout : AbstractViewModel
    {
        private ICollection<LengthDefinitionSetting> _rowsHeight;
        private ICollection<IViewModel> _viewModels;

        public MultipleRowsLayout(ICollection<LengthDefinitionSetting> rowsHeight, ICollection<IViewModel> viewModels, bool addGridSplitters)
        {
            AddGridSplitters = addGridSplitters;
            _rowsHeight = rowsHeight;
            _viewModels = viewModels;
        }

        public MultipleRowsLayout()
        {
        }

        public bool AddGridSplitters { get; set; }

        public ICollection<IViewModel> ViewModels
        {
            get => _viewModels;
            set => SetProperty(ref _viewModels, value);
        }

        public ICollection<LengthDefinitionSetting> RowsHeight
        {
            get => _rowsHeight;
            set => SetProperty(ref _rowsHeight, value);
        }
    }
}