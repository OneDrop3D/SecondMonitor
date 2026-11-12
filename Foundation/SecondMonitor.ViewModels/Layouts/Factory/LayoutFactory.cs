namespace SecondMonitor.ViewModels.Layouts.Factory
{
    using System;
    using System.Linq;
    using Editor;
    using Settings.Model.Layout;

    public class LayoutFactory : ILayoutFactory
    {
        public IViewModel Create(GenericContentSetting rootElement, IViewModelByNameFactory viewModelByNameFactory)
        {
            switch (rootElement)
            {
                case NamedContentSetting namedContent:
                    return CreateNamedContent(namedContent, viewModelByNameFactory);
                case RowsDefinitionSetting rowsDefinition:
                    return CreateRowsViewModel(rowsDefinition, viewModelByNameFactory);
                case ColumnsDefinitionSetting columnsDefinition:
                    return CreateColumnsViewModel(columnsDefinition, viewModelByNameFactory);
                case null:
                    return new EmptyViewModel();
                default:
                    throw new NotSupportedException($"Cannot create layout with for settings element {rootElement.GetType()}");
            }
        }

        private IViewModel CreateNamedContent(NamedContentSetting namedContent, IViewModelByNameFactory viewModelByNameFactory)
        {
            IViewModel createdViewModel = viewModelByNameFactory.Create(namedContent.ContentName);
            return WrapViewModelIfNecessary(namedContent, createdViewModel);
        }

        private IViewModel CreateRowsViewModel(RowsDefinitionSetting rowsDefinition, IViewModelByNameFactory viewModelByNameFactory)
        {
            IViewModel viewModel = new MultipleRowsLayout(rowsDefinition.RowsSize.ToList(), rowsDefinition.RowsContent.Select(x => Create(x, viewModelByNameFactory)).ToList(), rowsDefinition.AddGridSplitters);
            return WrapViewModelIfNecessary(rowsDefinition, viewModel);
        }

        private IViewModel CreateColumnsViewModel(ColumnsDefinitionSetting columnsDefinition, IViewModelByNameFactory viewModelByNameFactory)
        {
            IViewModel viewModel = new MultipleColumnsLayout(columnsDefinition.ColumnsSize.ToList(), columnsDefinition.ColumnsContent.Select(x => Create(x, viewModelByNameFactory)).ToList(), columnsDefinition.AddGridSplitter);
            return WrapViewModelIfNecessary(columnsDefinition, viewModel);
        }

        private IViewModel WrapViewModelIfNecessary(GenericContentSetting genericContentSetting, IViewModel wrappedViewModel)
        {
            IViewModel toReturn = wrappedViewModel;
            if (genericContentSetting.IsExpanderEnabled)
            {
                toReturn = new ExpanderWrapper(wrappedViewModel, genericContentSetting.ExpandDirection, genericContentSetting.IsExpanderExpanded);
            }

            if (genericContentSetting.IsCustomHeight || genericContentSetting.IsCustomWidth)
            {
                toReturn = new CustomSizeWrapper(genericContentSetting.IsCustomWidth ? genericContentSetting.CustomWidth : double.NaN, genericContentSetting.IsCustomHeight ? genericContentSetting.CustomHeight : double.NaN, toReturn);
            }

            return toReturn;
        }
    }
}