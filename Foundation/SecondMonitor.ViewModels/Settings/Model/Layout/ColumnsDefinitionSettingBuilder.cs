namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System.Collections.Generic;

    public class ColumnsDefinitionSettingBuilder
    {
        private readonly List<LengthDefinitionSetting> _lengthSettings;
        private readonly List<GenericContentSetting> _content;
        private bool _withGridSplitters;
        private ColumnsDefinitionSettingBuilder()
        {
            _lengthSettings = new List<LengthDefinitionSetting>();
            _content = new List<GenericContentSetting>();
        }

        public static ColumnsDefinitionSettingBuilder Create()
        {
            return new ColumnsDefinitionSettingBuilder();
        }

        public ColumnsDefinitionSettingBuilder WithNamedContent(string contentName)
        {
            return WithNamedContent(contentName, SizeKind.Automatic, 100);
        }

        public ColumnsDefinitionSettingBuilder WithNamedContentCustomWidth(string contentName, int contentWidth)
        {
            var namedContent = new NamedContentSetting()
            {
                ContentName = contentName,
                IsCustomHeight = true,
                CustomHeight = contentWidth,
            };
            return WithContent(namedContent, SizeKind.Automatic, 100);
        }

        public ColumnsDefinitionSettingBuilder WithNamedContent(string contentName, SizeKind size, int manualSize)
        {
            _lengthSettings.Add(new LengthDefinitionSetting(size, manualSize));
            _content.Add(new NamedContentSetting(contentName));
            return this;
        }

        public ColumnsDefinitionSettingBuilder WithContent(GenericContentSetting genericContentSetting)
        {
            return WithContent(genericContentSetting, SizeKind.Automatic, 100);
        }

        public ColumnsDefinitionSettingBuilder WithContent(GenericContentSetting genericContentSetting, SizeKind size, int manualSize)
        {
            _lengthSettings.Add(new LengthDefinitionSetting(size, manualSize));
            _content.Add(genericContentSetting);
            return this;
        }

        public ColumnsDefinitionSettingBuilder WithGridSplitters()
        {
            _withGridSplitters = true;
            return this;
        }

        public ColumnsDefinitionSetting Build()
        {
            return new ColumnsDefinitionSetting(_content.Count, _lengthSettings.ToArray(), _content.ToArray(), _withGridSplitters);
        }
    }
}