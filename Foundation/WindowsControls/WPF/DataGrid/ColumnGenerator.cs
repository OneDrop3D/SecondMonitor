namespace SecondMonitor.WindowsControls.WPF.DataGrid
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using NLog;
    using ViewModels.DataGrid;
    using ViewModels.Settings.Model.Layout;

    internal class ColumnGenerator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly int _defaultFontSize;

        internal ColumnGenerator(int defaultFontSize)
        {
            _defaultFontSize = defaultFontSize;
        }

        internal DataGridColumn Generate(ColumnDescriptor descriptor)
        {
            switch (descriptor)
            {
                case BindableDataColumnDescriptor bindableDataColumnDescriptor:
                    return GenerateContentPresenterColumn(bindableDataColumnDescriptor);
                case BlankColumnDescriptor blankColumnDescriptor:
                    return GenerateBlankColumn(blankColumnDescriptor);
                case TemplateTextColumnDescriptor templateTextColumnDescriptor:
                    return GenerateTemplatedTextColumn(templateTextColumnDescriptor);
                case TextColumnDescriptor textColumnDescriptor:
                    return GenerateTextColumn(textColumnDescriptor);
                case TemplatedColumnDescriptor templatedColumnDescriptor:
                    return GenerateTemplatedColumn(templatedColumnDescriptor);
                default: throw new ArgumentException($"Unknown ColumnType {descriptor.GetType()}");
            }
        }

        private DataGridColumn GenerateContentPresenterColumn(BindableDataColumnDescriptor bindableDataColumnDescriptor)
        {
            Binding binding = new Binding(bindableDataColumnDescriptor.BindingPropertyName)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            ContentPresenterGridColumn newColumn = new ContentPresenterGridColumn()
            {
                Binding = binding,
            };

            SetColumn(newColumn, bindableDataColumnDescriptor);
            return newColumn;
        }

        private DataGridColumn GenerateBlankColumn(BlankColumnDescriptor blankColumnDescriptor)
        {
            BlankColumn newColumn = new BlankColumn(blankColumnDescriptor.BackgroundColor.ToSolidColorBrush());
            SetColumn(newColumn, blankColumnDescriptor);
            return newColumn;
        }

        private DataGridColumn GenerateTemplatedTextColumn(TemplateTextColumnDescriptor templateTextColumnDescriptor)
        {
            DataGridTemplateColumn newColumn = new TemplateTextColumn(templateTextColumnDescriptor.UseCustomFontSize ? templateTextColumnDescriptor.FontSize : _defaultFontSize,
                templateTextColumnDescriptor.IsBold ? FontWeights.ExtraBold : FontWeights.Normal,
                templateTextColumnDescriptor.IsItalic ? FontStyles.Italic : FontStyles.Normal);

            if (Application.Current.Resources.Contains(templateTextColumnDescriptor.CellTemplateName))
            {
                newColumn.CellTemplate = (DataTemplate)Application.Current.Resources[templateTextColumnDescriptor.CellTemplateName];
            }
            else
            {
                Logger.Error($"Unable to find cell template {templateTextColumnDescriptor.CellTemplateName}");
            }

            SetColumn(newColumn, templateTextColumnDescriptor);
            return newColumn;
        }

        private DataGridColumn GenerateTemplatedColumn(TemplatedColumnDescriptor templatedColumnDescriptor)
        {
            DataGridTemplateColumn newColumn = new DataGridTemplateColumn();
            if (Application.Current.Resources.Contains(templatedColumnDescriptor.CellTemplateName))
            {
                newColumn.CellTemplate = (DataTemplate)Application.Current.Resources[templatedColumnDescriptor.CellTemplateName];
            }
            else
            {
                Logger.Error($"Unable to find cell template {templatedColumnDescriptor.CellTemplateName}");
            }

            SetColumn(newColumn, templatedColumnDescriptor);
            return newColumn;
        }

        private DataGridColumn GenerateTextColumn(TextColumnDescriptor textColumnDescriptor)
        {
            Binding binding = new Binding(textColumnDescriptor.BindingPropertyName)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            DataGridTextColumn newColumn = new DataGridTextColumn()
            {
                Binding = binding,
            };

            if (textColumnDescriptor.UseCustomFontSize)
            {
                newColumn.FontSize = textColumnDescriptor.FontSize;
            }

            if (textColumnDescriptor.IsBold)
            {
                newColumn.FontWeight = FontWeights.ExtraBold;
            }

            if (textColumnDescriptor.IsItalic)
            {
                newColumn.FontStyle = FontStyles.Italic;
            }

            SetColumn(newColumn, textColumnDescriptor);

            if (!string.IsNullOrEmpty(textColumnDescriptor.CustomElementStyle))
            {
                newColumn.ElementStyle = (Style)Application.Current.Resources[textColumnDescriptor.CustomElementStyle];
            }

            return newColumn;
        }

        private void SetColumn(DataGridColumn customColumn, ColumnDescriptor descriptor)
        {
            customColumn.Header = descriptor.Title;
            customColumn.CanUserResize = descriptor.CanResize;
            customColumn.Width = descriptor.ColumnLength.SizeKind == SizeKind.Manual ? new DataGridLength(descriptor.ColumnLength.ManualSize) : DataGridLength.Auto;
        }
    }
}