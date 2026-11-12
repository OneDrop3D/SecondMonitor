namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System.IO;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using Common.DataModel.Championship.Calendar;
    using SecondMonitor.ViewModels.Settings;

    public class StoredCalendarAccessor : IStoredCalendarsAccessor
    {
        private readonly ISettingsProvider _settingsProvider;

        public StoredCalendarAccessor(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public bool TrySaveCalendar(CalendarDto calendarDto)
        {
            using SaveFileDialog saveFileDialog = new SaveFileDialog();
            string exportDirectory = _settingsProvider.ChampionshipCalendarsPath;
            if (!Directory.Exists(exportDirectory))
            {
                Directory.CreateDirectory(exportDirectory);
            }

            saveFileDialog.InitialDirectory = exportDirectory;
            saveFileDialog.FileName = $"{calendarDto.Name} ({calendarDto.Simulator})";
            saveFileDialog.DefaultExt = ".smc";
            saveFileDialog.Filter = @"Second Monitor Calendar (*.smc)|*.smc";
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
            {
                XmlSerializer xmlSerializer = new(typeof(CalendarDto));

                using (FileStream file = File.Exists(saveFileDialog.FileName) ? File.Open(saveFileDialog.FileName, FileMode.Truncate) : File.Create(saveFileDialog.FileName))
                {
                    xmlSerializer.Serialize(file, calendarDto);
                    file.Close();
                }

                return true;
            }

            return false;
        }

        public bool TryLoadCalendar(out CalendarDto calendarDto)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            string exportDirectory = _settingsProvider.ChampionshipCalendarsPath;
            if (!Directory.Exists(exportDirectory))
            {
                Directory.CreateDirectory(exportDirectory);
            }

            openFileDialog.InitialDirectory = exportDirectory;
            openFileDialog.DefaultExt = ".smc";
            openFileDialog.Filter = @"Second Monitor Calendar (*.smc)|*.smc";
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                XmlSerializer xmlSerializer = new(typeof(CalendarDto));

                using (FileStream file = File.Open(openFileDialog.FileName, FileMode.Open))
                {
                    calendarDto = (CalendarDto)xmlSerializer.Deserialize(file);
                    file.Close();
                }

                return true;
            }

            calendarDto = null;
            return false;
        }
    }
}