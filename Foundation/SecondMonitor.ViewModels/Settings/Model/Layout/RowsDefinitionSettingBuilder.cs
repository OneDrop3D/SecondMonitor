namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System.Collections.Generic;

    public class RowsDefinitionSettingBuilder
    {
        private readonly List<LengthDefinitionSetting> _lengthSettings;
        private readonly List<GenericContentSetting> _content;
        private bool _withGridSplitters;
        private RowsDefinitionSettingBuilder()
        {
            _lengthSettings = new List<LengthDefinitionSetting>();
            _content = new List<GenericContentSetting>();
        }

        public static RowsDefinitionSettingBuilder Create()
        {
            return new RowsDefinitionSettingBuilder();
        }

        public RowsDefinitionSettingBuilder WithNamedContent(string contentName)
        {
            return WithNamedContent(contentName, SizeKind.Automatic, 100);
        }

        public RowsDefinitionSettingBuilder WithNamedContentCustomHeight(string contentName, int contentHeight)
        {
            var namedContent = new NamedContentSetting()
            {
                ContentName = contentName,
                IsCustomHeight = true,
                CustomHeight = contentHeight,
            };
            return WithContent(namedContent, SizeKind.Automatic, 100);
        }

        public RowsDefinitionSettingBuilder WithNamedContent(string contentName, SizeKind size, int manualSize)
        {
            _lengthSettings.Add(new LengthDefinitionSetting(size, manualSize));
            _content.Add(new NamedContentSetting(contentName));
            return this;
        }

        public RowsDefinitionSettingBuilder WithContent(GenericContentSetting genericContentSetting)
        {
            return WithContent(genericContentSetting, SizeKind.Automatic, 100);
        }

        public RowsDefinitionSettingBuilder WithContent(GenericContentSetting genericContentSetting, SizeKind size, int manualSize)
        {
            _lengthSettings.Add(new LengthDefinitionSetting(size, manualSize));
            _content.Add(genericContentSetting);
            return this;
        }

        public RowsDefinitionSettingBuilder WithGridSplitters()
        {
            _withGridSplitters = true;
            return this;
        }

        public RowsDefinitionSetting Build()
        {
            return new RowsDefinitionSetting(_content.Count, _lengthSettings.ToArray(), _content.ToArray(), _withGridSplitters);
        }
    }
}