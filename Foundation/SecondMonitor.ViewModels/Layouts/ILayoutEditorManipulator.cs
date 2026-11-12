namespace SecondMonitor.ViewModels.Layouts
{
    using System.Collections.Generic;
    using System.Windows.Input;
    using Editor;
    using Settings.Model.Layout;

    public interface ILayoutEditorManipulator
    {
        ICommand ApplyLayoutCommand { get; set; }

        LayoutEditorViewModel LayoutEditorViewModel { get; }

        LengthDefinitionSetting NewRowSize { get; set; }

        LengthDefinitionSetting NewColumnSize { get; set; }

        List<string> GetAllowableContentNames();

        void Select(ILayoutConfigurationViewModel layoutElement);
        LayoutDescription CreateDefaultLayout();

        void SetLayoutDescription(LayoutDescription layoutDescription);
        LayoutDescription GetLayoutDescription();

        void RegisterLayoutContainer(ILayoutContainer layoutContainer);

        void UnRegisterLayoutContainer(ILayoutContainer layoutContainer);

        void ClearLayoutContainers();

        void RemoveLayoutElement(ILayoutConfigurationViewModel layoutElement);

        void ReplaceForNamedContent(ILayoutConfigurationViewModel layoutElement);
        void ReplaceForRows(ILayoutConfigurationViewModel layoutElement);
        void ReplaceForColumns(ILayoutConfigurationViewModel layoutElement);
        (GenericContentSetting Content, LengthDefinitionSetting RowSize) CreateDefaultRowLayout();
        (GenericContentSetting Content, LengthDefinitionSetting ColumnSize) CreateDefaultColumnLayout();
    }
}