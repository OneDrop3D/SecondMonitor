namespace SecondMonitor.ViewModels.Settings.ViewModel.Columns
{
    using System;
    using DataGrid;
    using Factory;

    public class ColumnDescriptorViewModelFactory
    {
        private readonly IViewModelFactory _viewModelFactory;

        public ColumnDescriptorViewModelFactory(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public ColumnDescriptorViewModel CreateAndApplyModel(ColumnDescriptor columnDescriptor)
        {
            ColumnDescriptorViewModel viewModel;
            switch (columnDescriptor)
            {
                case TemplateTextColumnDescriptor _:
                    viewModel = _viewModelFactory.Create<TextColumnDescriptorViewModel>();
                    break;
                case TextColumnDescriptor _:
                    viewModel = _viewModelFactory.Create<TextColumnDescriptorViewModel>();
                    break;
                case TemplatedColumnDescriptor _:
                    viewModel = _viewModelFactory.Create<TemplateColumnDescriptorViewModel>();
                    break;
                case BlankColumnDescriptor _:
                    viewModel = _viewModelFactory.Create<BlankColumnDescriptorViewModel>();
                    break;
                default:
                    throw new ArgumentException($"Unable to create viewmodel for {columnDescriptor.GetType()}");
            }

            viewModel.FromModel(columnDescriptor);
            return viewModel;
        }
    }
}