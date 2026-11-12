namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System;
    using Settings.Model.Layout;
    using ViewModels.Factory;

    public class LayoutConfigurationViewModelFactory : ILayoutConfigurationViewModelFactory
    {
        private readonly IViewModelFactory _viewModelFactory;

        public LayoutConfigurationViewModelFactory(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        public ILayoutConfigurationViewModel Create(GenericContentSetting rootElement)
        {
            switch (rootElement)
            {
                case NamedContentSetting namedContent:
                    return CreateNamedContent(namedContent);
                case RowsDefinitionSetting rowsDefinition:
                    return CreateRowsViewModel(rowsDefinition);
                case ColumnsDefinitionSetting columnsDefinition:
                    return CreateColumnsViewModel(columnsDefinition);
                case null:
                    return CreateEmpty();
                default:
                    throw new NotSupportedException($"Cannot create layout editor for settings element {rootElement.GetType()}");
            }
        }

        public ILayoutConfigurationViewModel CreateEmpty()
        {
            return new EmptyLayoutElementViewModel()
            {
                LayoutEditorManipulator = LayoutEditorManipulator,
                LayoutConfigurationViewModelFactory = this,
            };
        }

        public GenericContentSetting ToContentSettings(ILayoutConfigurationViewModel layoutViewModel)
        {
            switch (layoutViewModel)
            {
                case NamedContentViewModel namedContent:
                    return ToNamedContentSetting(namedContent);
                case RowsDefinitionSettingViewModel rowsDefinition:
                    return ToRowsSettings(rowsDefinition);
                case ColumnsDefinitionSettingViewModel columnsDefinition:
                    return ToColumnsSettings(columnsDefinition);
                case EmptyLayoutElementViewModel _:
                    return null;
                default:
                    throw new NotSupportedException($"Cannot create layout editor for settings element {layoutViewModel.GetType()}");
            }
        }

        private static GenericContentSetting ToColumnsSettings(ColumnsDefinitionSettingViewModel columnsDefinition)
        {
            return columnsDefinition.SaveToNewModel();
        }

        private static GenericContentSetting ToRowsSettings(RowsDefinitionSettingViewModel rowsDefinition)
        {
            return rowsDefinition.SaveToNewModel();
        }

        private static NamedContentSetting ToNamedContentSetting(NamedContentViewModel namedContentViewModel)
        {
            return namedContentViewModel.SaveToNewModel();
        }

        private ILayoutConfigurationViewModel CreateNamedContent(NamedContentSetting namedContent)
        {
            NamedContentViewModel viewModel = _viewModelFactory.Create<NamedContentViewModel>();
            viewModel.LayoutConfigurationViewModelFactory = this;
            viewModel.LayoutEditorManipulator = LayoutEditorManipulator;
            viewModel.FromModel(namedContent);
            return viewModel;
        }

        private ILayoutConfigurationViewModel CreateRowsViewModel(RowsDefinitionSetting rowsDefinition)
        {
            RowsDefinitionSettingViewModel viewModel = _viewModelFactory.Create<RowsDefinitionSettingViewModel>();
            viewModel.LayoutConfigurationViewModelFactory = this;
            viewModel.LayoutEditorManipulator = LayoutEditorManipulator;
            viewModel.FromModel(rowsDefinition);
            return viewModel;
        }

        private ILayoutConfigurationViewModel CreateColumnsViewModel(ColumnsDefinitionSetting columnsDefinition)
        {
            ColumnsDefinitionSettingViewModel viewModel = _viewModelFactory.Create<ColumnsDefinitionSettingViewModel>();
            viewModel.LayoutConfigurationViewModelFactory = this;
            viewModel.LayoutEditorManipulator = LayoutEditorManipulator;
            viewModel.FromModel(columnsDefinition);
            return viewModel;
        }
    }
}