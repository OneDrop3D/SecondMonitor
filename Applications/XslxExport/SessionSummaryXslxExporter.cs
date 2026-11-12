namespace SecondMonitor.XslxExport
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Media;
    using Contracts.Statistics;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using DataModel.Summary;
    using NLog;
    using OfficeOpenXml;
    using OfficeOpenXml.Drawing;
    using OfficeOpenXml.Drawing.Chart;
    using OfficeOpenXml.Style;
    using OfficeOpenXml.Table;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Summary.FuelConsumption;

    using ViewModels.Colors.Extensions;

    public class SessionSummaryXslxExporter
    {
        private const string SummarySheet = "Summary";
        private const string LapsAndSectorsSheet = "Laps & Sectors";
        private const string PlayerLapsSheet = "Players Laps";
        private const string RaceProgressSheet = "Race Progress";
        private const string TyreDegradationSheet = "Tyres Condition";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static SessionSummaryXslxExporter()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public Color PersonalBestColor { get; set; } = Colors.Green;

        public Color SessionBestColor { get; set; } = Colors.Purple;

        public Color InvalidColor { get; set; } = Color.FromRgb(189, 7, 59);

        public VelocityUnits VelocityUnits { get; set; } = VelocityUnits.Kph;

        public TemperatureUnits TemperatureUnits { get; set; } = TemperatureUnits.Celsius;

        public VolumeUnits VolumeUnits { get; set; } = VolumeUnits.UsGallons;

        public PressureUnits PressureUnits { get; set; } = PressureUnits.Kpa;

        public DistanceUnits DistanceUnits { get; set; } = DistanceUnits.Miles;

        public Color RatingHigher { get; set; } = Colors.Red;

        public Color RatingLowerOrSame { get; set; } = Colors.Green;

        public bool IncludeRatings { get; set; } = false;

        public int NumberOfSummaryColumns { get; set; } = 12;

        public int SummaryInfoBoxesFirstColumn { get; set; }

        public void ExportSessionSummary(SessionSummary sessionSummary, string filePath, bool includeRatings = true)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                FileInfo newFile = new FileInfo(filePath);
                ExcelPackage package = new ExcelPackage(newFile);

                IncludeRatings = includeRatings && sessionSummary.Drivers.Exists(x => x.Rating != 0);
                if (IncludeRatings)
                {
                    NumberOfSummaryColumns++;
                }

                SummaryInfoBoxesFirstColumn = NumberOfSummaryColumns + 2;

                CreateWorkBook(package, sessionSummary.SessionType == SessionType.Race);
                ExcelWorkbook workbook = package.Workbook;
                AddSummary(workbook.Worksheets[SummarySheet], sessionSummary);
                AddLapsInfo(workbook.Worksheets[LapsAndSectorsSheet], sessionSummary);
                AddPlayersLaps(workbook.Worksheets[PlayerLapsSheet], sessionSummary);
                AddTyresDegradation(workbook.Worksheets[TyreDegradationSheet], sessionSummary);

                if (sessionSummary.SessionType == SessionType.Race)
                {
                    AddRaceProgress(workbook.Worksheets[RaceProgressSheet], sessionSummary);
                }

                package.Save();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to export session info");
            }
        }

        private static void CreateWorkBook(ExcelPackage package, bool includeRaceProgress)
        {
            package.Workbook.Worksheets.Add(SummarySheet);
            package.Workbook.Worksheets.Add(LapsAndSectorsSheet);
            package.Workbook.Worksheets.Add(PlayerLapsSheet);

            if (includeRaceProgress)
            {
                package.Workbook.Worksheets.Add(RaceProgressSheet);
            }

            package.Workbook.Worksheets.Add(TyreDegradationSheet);
        }

        private static string GetFinishPositionInfo(Driver driver, bool addStartPosition)
        {
            string endPosition = driver.Finished ? driver.FinishingPosition.ToString() : "DNF";
            if (!addStartPosition)
            {
                return endPosition;
            }

            Lap firstLap = driver.Laps.LastOrDefault(x => x.LapNumber == 1);
            return firstLap?.LapStartSnapshot == null ? endPosition : $"{endPosition} (Started: {firstLap.LapStartSnapshot.PlayerData.Position})";
        }

        private static void WrapSummaryInTable(ExcelWorksheet sheet, int rowCount, int numberOfSummaryColumns)
        {
            ExcelRange range = sheet.Cells[5, 1, 5 + rowCount, numberOfSummaryColumns];
            ExcelTable table = sheet.Tables.Add(range, "SummaryTable");
            table.ShowHeader = true;
            for (int i = 1; i <= 10; i++)
            {
                sheet.Column(i).AutoFit();
            }

            if (numberOfSummaryColumns > 12)
            {
                for (int i = 12 + 1; i <= numberOfSummaryColumns; i++)
                {
                    sheet.Column(i).AutoFit();
                }
            }
        }

        private static void FillWithColor(ExcelRange range, Color foregroundColor, Color bgColor)
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Font.Color.SetColor(foregroundColor.ToDrawingColor());
            range.Style.Fill.BackgroundColor.SetColor(bgColor.ToDrawingColor());
            range.Style.Fill.PatternColor.SetColor(bgColor.ToDrawingColor());
        }

        private void AddRaceProgress(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            int maxLaps = sessionSummary.Drivers.Select(x => x.Laps.Count).Max();
            List<Driver> orderedDrivers = sessionSummary.Drivers.OrderBy(x => x.Laps.LastOrDefault()?.LapEndSnapshot.PlayerData.Position).ToList();
            int startRow = 100;
            int startColumn = 1;
            sheet.Cells[startRow + 1, startColumn].Value = "Start";
            GenerateNumberColumn(sheet, new ExcelCellAddress(startRow + 2, startColumn), maxLaps);
            GenerateDriversRow(sheet, new ExcelCellAddress(startRow, startColumn + 1), orderedDrivers.Select(x => x.DriverLongName));

            ExcelCellAddress startAddress = new ExcelCellAddress(startRow + 1, startColumn + 1);
            orderedDrivers.ForEach(
                x =>
                    {
                        GenerateLapsPositionColumn(sheet, startAddress, x.Laps, maxLaps);
                        startAddress = new ExcelCellAddress(startAddress.Row, startAddress.Column + 1);
                    });
            ExcelLineChart chart = (ExcelLineChart)sheet.Drawings.AddChart("Race Progress", eChartType.LineMarkers);
            chart.SetPosition(0, 0, 0, 0);
            int currentColumn = 2;
            orderedDrivers.ForEach(
                x =>
                    {
                        ExcelLineChartSerie series = (ExcelLineChartSerie)chart.Series.Add(ExcelCellBase.GetAddress(startRow + 1, currentColumn, startRow + 1 + maxLaps, currentColumn), ExcelCellBase.GetAddress(startRow + 1, 1, startRow + 1 + maxLaps, 1));
                        series.Header = x.DriverLongName;
                        currentColumn++;
                    });
            chart.ShowDataLabelsOverMaximum = false;
            chart.DataLabel.ShowValue = true;
            chart.ShowHiddenData = true;

            chart.Axis[1].MinValue = 0;
            chart.Axis[1].TickLabelPosition = eTickLabelPosition.NextTo;
            chart.Axis[1].MajorUnit = 1;
            chart.Axis[1].MinorUnit = 1;
            chart.Axis[1].Orientation = eAxisOrientation.MaxMin;
            chart.Axis[1].MaxValue = orderedDrivers.Count;
            chart.SetSize(70 * maxLaps, 30 * orderedDrivers.Count);
            chart.Axis[0].MajorUnit = 1;
            chart.Axis[0].MinorUnit = 1;
            chart.Title.Text = "Race Progress";
        }

        private void GenerateLapsPositionColumn(ExcelWorksheet sheet, ExcelCellAddress startAddress, IEnumerable<Lap> laps, int maxLaps)
        {
            List<Lap> lapsList = laps.ToList();
            if (!lapsList.Any())
            {
                return;
            }

            sheet.Cells[startAddress.Address].Value = lapsList.First().LapStartSnapshot.PlayerData.Position;
            startAddress = new ExcelCellAddress(startAddress.Row + 1, startAddress.Column);

            lapsList.ForEach(x =>
            {
                sheet.Cells[startAddress.Address].Value = x.LapEndSnapshot?.PlayerData?.Position > 0 ? x.LapEndSnapshot.PlayerData.Position : x.Driver.FinishingPosition;
                startAddress = new ExcelCellAddress(startAddress.Row + 1, startAddress.Column);
            });

            for (int i = lapsList.Count(); i < maxLaps; i++)
            {
                sheet.Cells[startAddress.Address].Value = lapsList.Last().Driver.FinishingPosition;
                startAddress = new ExcelCellAddress(startAddress.Row + 1, startAddress.Column);
            }
        }

        private void GenerateNumberColumn(ExcelWorksheet sheet, ExcelCellAddress cellAddress, int count)
        {
            for (int i = 0; i < count; i++)
            {
                sheet.Cells[cellAddress.Row + i, cellAddress.Column].Value = i + 1;
            }
        }

        private void GenerateNumberColumn(ExcelWorksheet sheet, ExcelCellAddress cellAddress, List<Lap> laps)
        {
            for (int i = 0; i < laps.Count; i++)
            {
                sheet.Cells[cellAddress.Row + i, cellAddress.Column].Value = laps[i].LapNumber;
            }
        }

        private void GenerateDriversRow(ExcelWorksheet sheet, ExcelCellAddress cellAddress, IEnumerable<string> driverNames)
        {
            driverNames.ForEach(x =>
                {
                    sheet.Cells[cellAddress.Address].Value = x;
                    sheet.Cells[cellAddress.Address].AutoFitColumns();
                    cellAddress = new ExcelCellAddress(cellAddress.Row, cellAddress.Column + 1);
                });
        }

        private void AddPlayersLaps(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            AddPlayerLapsHeader(sheet);
            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (player == null)
            {
                return;
            }

            Lap lastLap = null;
            int currentRow = 6;
            List<Lap> playerLaps = player.Laps;
            List<Lap> playerLapsForGraph = playerLaps.Where(x => x.LapTime.TotalSeconds > 0 && !x.IsPitLap && (sessionSummary.SessionType != SessionType.Race || x.LapNumber > 1)).ToList();
            playerLaps.Where(l => l.LapEndSnapshot != null).ToList().ForEach(
                x =>
                    {
                        currentRow = AddPlayerLapInfo(sheet, sessionSummary, x, lastLap, currentRow);
                        lastLap = x;
                    });
            sheet.Cells["A1:M5"].AutoFitColumns();

            int totalLaps = playerLapsForGraph.Count;
            if (totalLaps == 0)
            {
                return;
            }

            int startRow = 100;
            int startColumn = 20;
            sheet.Cells[startRow, startColumn].Value = "Start";
            sheet.Cells[startRow, startColumn + 1].Value = "Lap Time";
            GenerateNumberColumn(sheet, new ExcelCellAddress(startRow + 1, startColumn), playerLapsForGraph);
            GenerateLapTimesRows(sheet, new ExcelCellAddress(startRow + 1, startColumn + 1), playerLapsForGraph);

            StandardDeviationResult<double> standardDeviationValidLaps = StandardDeviationCalculator.ComputeDeviation(playerLapsForGraph.Where(x => x.IsValid).Select(x => x.LapTime.TotalSeconds).ToList());
            StandardDeviationResult<double> standardDeviationAllLaps = StandardDeviationCalculator.ComputeDeviation(playerLapsForGraph.Select(x => x.LapTime.TotalSeconds).ToList());
            sheet.Cells[8, 15].Value = "Valid Laps Std. dev.:";
            sheet.Cells[9, 15].Value = "All Laps Std. dev.:";
            sheet.Cells[8, 16].Value = double.IsNaN(standardDeviationValidLaps.MeanValue) ? "-" : standardDeviationValidLaps.StandardDeviation.ToString("F2");
            sheet.Cells[9, 16].Value = double.IsNaN(standardDeviationAllLaps.MeanValue) ? "-" : standardDeviationAllLaps.StandardDeviation.ToString("F2");

            sheet.Cells[8, 18].Value = "Average:";
            sheet.Cells[9, 18].Value = "Average:";
            sheet.Cells[8, 19].Value = double.IsNaN(standardDeviationValidLaps.MeanValue) ? "-" : FormatTimeSpan(TimeSpan.FromSeconds(standardDeviationValidLaps.MeanValue));
            sheet.Cells[9, 19].Value = double.IsNaN(standardDeviationAllLaps.MeanValue) ? "-" : FormatTimeSpan(TimeSpan.FromSeconds(standardDeviationAllLaps.MeanValue));

            sheet.Cells[8, 15, 9, 19].AutoFitColumns();

            ExcelLineChart chart = (ExcelLineChart)sheet.Drawings.AddChart("Lap times", eChartType.LineMarkers);
            chart.SetPosition(10, 0, 14, 0);

            ExcelLineChartSerie series = (ExcelLineChartSerie)chart.Series.Add(ExcelCellBase.GetAddress(startRow + 1, 21, startRow + totalLaps, 21), ExcelCellBase.GetAddress(startRow + 1, 20, startRow + totalLaps, 20));
            series.Header = "Lap Time";

            chart.ShowDataLabelsOverMaximum = false;
            chart.DataLabel.ShowValue = true;
            chart.ShowHiddenData = true;

            chart.Axis[1].MinValue = Math.Floor(playerLapsForGraph.Select(x => x.LapTime.TotalSeconds).Min() - 10);
            chart.Axis[1].TickLabelPosition = eTickLabelPosition.NextTo;
            chart.Axis[1].MajorUnit = 5;
            chart.Axis[1].MajorGridlines.LineStyle = eLineStyle.Dash;
            chart.Axis[1].MinorGridlines.LineStyle = eLineStyle.Dot;
            chart.Axis[1].MinorUnit = 1;
            chart.Axis[1].Orientation = eAxisOrientation.MinMax;
            chart.Axis[1].MaxValue = Math.Ceiling(playerLapsForGraph.Select(x => x.LapTime.TotalSeconds).Max() + 10);
            chart.SetSize(Math.Max(70 * totalLaps, 600), 400);
            chart.Axis[0].MajorUnit = 1;
            chart.Axis[0].MinorUnit = 1;
            chart.Title.Text = "Player Laps (without pit laps)";
        }

        private void AddLapsInfo(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            AddLapsInfoHeader(sheet, sessionSummary);
            int currentColumn = 2;
            foreach (Driver driver in sessionSummary.Drivers.Where(d => d.Finished).OrderBy(o => o.FinishingPosition))
            {
                AddDriverLaps(sheet, currentColumn, driver, sessionSummary);
                currentColumn = currentColumn + 4;
            }

            foreach (Driver driver in sessionSummary.Drivers.Where(d => !d.Finished).OrderBy(d => d.TotalLaps).Reverse())
            {
                AddDriverLaps(sheet, currentColumn, driver, sessionSummary);
                currentColumn = currentColumn + 4;
            }
        }

        private void AddDriverLaps(ExcelWorksheet sheet, int startColumn, Driver driver, SessionSummary sessionSummary)
        {
            ExcelRange range = sheet.Cells[1, startColumn, 1, startColumn + 3];
            range.Merge = true;
            range.Value = driver.DriverLongName + "(" + (driver.Finished ? driver.FinishingPosition.ToString() : "DNF") + ")";
            range.Style.Font.Bold = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            sheet.Cells[2, startColumn].Value = "Sector 1";
            sheet.Cells[2, startColumn + 1].Value = "Sector 2";
            sheet.Cells[2, startColumn + 2].Value = "Sector 3";
            sheet.Cells[2, startColumn + 3].Value = "Lap";

            int currentRow = 3;
            foreach (var lap in driver.Laps.OrderBy(l => l.LapNumber))
            {
                sheet.Cells[currentRow, startColumn].Value = FormatTimeSpan(lap.Sector1);
                if (lap == driver.BestSector1Lap)
                {
                    FormatAsPersonalBest(sheet.Cells[currentRow, startColumn]);
                }

                if (lap == sessionSummary.SessionBestSector1)
                {
                    FormatAsSessionBest(sheet.Cells[currentRow, startColumn]);
                }

                sheet.Cells[currentRow, startColumn + 1].Value = FormatTimeSpan(lap.Sector2);
                if (lap == driver.BestSector2Lap)
                {
                    FormatAsPersonalBest(sheet.Cells[currentRow, startColumn + 1]);
                }

                if (lap == sessionSummary.SessionBestSector2)
                {
                    FormatAsSessionBest(sheet.Cells[currentRow, startColumn + 1]);
                }

                sheet.Cells[currentRow, startColumn + 2].Value = FormatTimeSpan(lap.Sector3);
                if (lap == driver.BestSector3Lap)
                {
                    FormatAsPersonalBest(sheet.Cells[currentRow, startColumn + 2]);
                }

                if (lap == sessionSummary.SessionBestSector3)
                {
                    FormatAsSessionBest(sheet.Cells[currentRow, startColumn + 2]);
                }

                sheet.Cells[currentRow, startColumn + 3].Value = FormatTimeSpan(lap.LapTime);
                if (lap == driver.BestPersonalLap)
                {
                    FormatAsPersonalBest(sheet.Cells[currentRow, startColumn + 3]);
                }

                if (lap == sessionSummary.SessionBestLap)
                {
                    FormatAsSessionBest(sheet.Cells[currentRow, startColumn + 3]);
                }

                if (!lap.IsValid)
                {
                    FillWithColor(
                        sheet.Cells[currentRow, startColumn, currentRow, startColumn + 3],
                        Colors.White,
                        InvalidColor);
                }

                currentRow++;
            }

            ExcelRange outLineRange = sheet.Cells[1, startColumn, currentRow - 1, startColumn + 3];
            outLineRange.Style.Border.BorderAround(ExcelBorderStyle.Thick, System.Drawing.Color.Black);
        }

        private void FormatAsPersonalBest(ExcelRange range)
        {
            FillWithColor(range, Colors.White, PersonalBestColor);
        }

        private void FormatAsSessionBest(ExcelRange range)
        {
            FillWithColor(range, Colors.White, SessionBestColor);
        }

        private void AddLapsInfoHeader(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            int maxLaps = sessionSummary.Drivers.Select(d => d.TotalLaps).Max();
            sheet.SelectedRange["A1"].Value = "Lap/Driver";
            for (int i = 1; i <= maxLaps; i++)
            {
                sheet.SelectedRange["A" + (i + 2)].Value = i;
            }

            ExcelRange range = sheet.SelectedRange["A1:A" + 2 + maxLaps];
            range.Style.Font.Bold = true;
            sheet.Column(1).AutoFit();
            sheet.View.FreezePanes(1, 2);
        }

        private void AddSummary(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            AddTrackInformation(sheet, sessionSummary);
            AddSessionBasicInformation(sheet, sessionSummary);
            AddDriversInfoHeader(sheet);
            AddDriversInfo(sheet, sessionSummary);
            WrapSummaryInTable(sheet, sessionSummary.Drivers.Count, NumberOfSummaryColumns);
            AddSessionBestInfo(sessionSummary, sheet);
            AddFuelConsumption(sessionSummary, player, sheet);
            AddPitsInfo(sessionSummary, sheet);
        }

        private void AddSessionBasicInformation(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            sheet.Cells["A2"].Value = "Date: " + sessionSummary.SessionRunTime.Date.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            sheet.Cells["A3"].Value = "Time: " + sessionSummary.SessionRunTime.TimeOfDay.ToString(@"hh\:mm");
            sheet.Cells["A4"].Value = "Simulator: " + sessionSummary.Simulator;
            sheet.Cells["B3"].Value = "Duration: " + sessionSummary.SessionRunDuration.FormatToHoursMinutesSecondsUnitSuffix();

            sheet.Cells[2, 1, 4, 1].AutoFitColumns();
        }

        private void AddDriversInfo(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            int rowNum = 5;
            foreach (Driver driver in sessionSummary.Drivers.Where(d => d.Finished).OrderBy(driver => driver.FinishingPosition))
            {
                ExcelRow row = sheet.Row(rowNum);
                AddDriverInfo(sheet, row, driver, sessionSummary);
                rowNum++;
            }

            foreach (Driver driver in sessionSummary.Drivers.Where(d => !d.Finished).OrderBy(d => d.TotalLaps).Reverse())
            {
                ExcelRow row = sheet.Row(rowNum);
                AddDriverInfo(sheet, row, driver, sessionSummary);
                rowNum++;
            }

            for (int i = 1; i <= 9; i++)
            {
                sheet.Column(i).AutoFit();
            }
        }

        private int AddPlayerLapInfo(ExcelWorksheet sheet, SessionSummary session, Lap lapInfo, Lap previousLap, int startRow)
        {
            CarInfo playerCarInfo = lapInfo.LapEndSnapshot.PlayerData.CarInfo;

            sheet.Cells[1 + startRow, 1].Value = lapInfo.IsValid ? lapInfo.LapNumber.ToString() : lapInfo.LapNumber + "(i)";
            sheet.Cells[1 + startRow, 2].Value = FormatTimeSpan(lapInfo.LapTime);

            if (lapInfo.Driver.BestPersonalLap != null)
            {
                TimeSpan lapDifference = lapInfo.LapTime - lapInfo.Driver.BestPersonalLap.LapTime;
                sheet.Cells[1 + startRow, 4].Value = (lapDifference.FormatTimeSpanOnlySeconds());
            }

            if (lapInfo == lapInfo.Driver.BestPersonalLap)
            {
                FormatAsPersonalBest(sheet.Cells[1 + startRow, 2]);
            }

            if (lapInfo == session.SessionBestLap)
            {
                FormatAsSessionBest(sheet.Cells[1 + startRow, 2]);
            }

            sheet.Cells[1 + startRow, 5].Value = playerCarInfo.FuelSystemInfo.FuelRemaining.GetValueInUnits(VolumeUnits, 1);
            sheet.Cells[1 + startRow, 7].Value = GetBrakeTemperature(playerCarInfo.WheelsInfo.FrontLeft);
            sheet.Cells[1 + startRow, 8].Value = playerCarInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity.GetValueInUnits(TemperatureUnits, 0);
            sheet.Cells[1 + startRow, 10].Value = GetBrakeTemperature(playerCarInfo.WheelsInfo.FrontRight);
            sheet.Cells[1 + startRow, 11].Value = playerCarInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity.GetValueInUnits(TemperatureUnits, 0);

            sheet.Cells[2 + startRow, 2].Value = FormatTimeSpan(lapInfo.Sector1);
            if (lapInfo == lapInfo.Driver.BestSector1Lap)
            {
                FormatAsPersonalBest(sheet.Cells[2 + startRow, 2]);
            }

            if (lapInfo == session.SessionBestSector1)
            {
                FormatAsSessionBest(sheet.Cells[2 + startRow, 2]);
            }

            sheet.Cells[2 + startRow, 3].Value = FormatTimeSpan(lapInfo.Sector2);
            if (lapInfo == lapInfo.Driver.BestSector2Lap)
            {
                FormatAsPersonalBest(sheet.Cells[2 + startRow, 3]);
            }

            if (lapInfo == session.SessionBestSector2)
            {
                FormatAsSessionBest(sheet.Cells[2 + startRow, 3]);
            }

            sheet.Cells[2 + startRow, 4].Value = FormatTimeSpan(lapInfo.Sector3);
            if (lapInfo == lapInfo.Driver.BestSector3Lap)
            {
                FormatAsPersonalBest(sheet.Cells[2 + startRow, 4]);
            }

            if (lapInfo == session.SessionBestSector3)
            {
                FormatAsSessionBest(sheet.Cells[2 + startRow, 4]);
            }

            if (previousLap != null)
            {
                sheet.Cells[2 + startRow, 5].Value =
                    (playerCarInfo.FuelSystemInfo.FuelRemaining
                     - previousLap.LapEndSnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining)
                    .GetValueInUnits(VolumeUnits, 2);
            }

            sheet.Cells[2 + startRow, 7].Value =
                playerCarInfo.WheelsInfo.FrontLeft.TyrePressure.ActualQuantity.GetValueInUnits(PressureUnits, 2);
            sheet.Cells[2 + startRow, 8].Value = ((1 - playerCarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear) * 100).ToString("F0");
            sheet.Cells[2 + startRow, 10].Value =
                playerCarInfo.WheelsInfo.FrontRight.TyrePressure.ActualQuantity.GetValueInUnits(PressureUnits, 2);
            sheet.Cells[2 + startRow, 11].Value = ((1 - playerCarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear) * 100).ToString("F0");

            sheet.Cells[4 + startRow, 2].Value =
                lapInfo.LapEndSnapshot.WeatherInfo.AirTemperature.GetValueInUnits(TemperatureUnits, 1);
            sheet.Cells[4 + startRow, 3].Value = lapInfo.LapEndSnapshot.WeatherInfo.TrackTemperature.GetValueInUnits(TemperatureUnits, 1);
            sheet.Cells[4 + startRow, 4].Value = lapInfo.LapEndSnapshot.WeatherInfo.RainIntensity;
            sheet.Cells[4 + startRow, 5].Value =
                playerCarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity.GetValueInUnits(TemperatureUnits, 1);
            sheet.Cells[4 + startRow, 7].Value = GetBrakeTemperature(playerCarInfo.WheelsInfo.RearLeft);
            sheet.Cells[4 + startRow, 8].Value = playerCarInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity.GetValueInUnits(TemperatureUnits, 0);
            sheet.Cells[4 + startRow, 10].Value = GetBrakeTemperature(playerCarInfo.WheelsInfo.RearRight);
            sheet.Cells[4 + startRow, 11].Value = playerCarInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity.GetValueInUnits(TemperatureUnits, 0);

            sheet.Cells[5 + startRow, 5].Value =
                playerCarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity.GetValueInUnits(TemperatureUnits, 1);
            sheet.Cells[5 + startRow, 7].Value =
                playerCarInfo.WheelsInfo.RearLeft.TyrePressure.ActualQuantity.GetValueInUnits(PressureUnits, 2);
            sheet.Cells[5 + startRow, 8].Value = ((1 - playerCarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear) * 100).ToString("F0");
            sheet.Cells[5 + startRow, 10].Value =
                playerCarInfo.WheelsInfo.RearRight.TyrePressure.ActualQuantity.GetValueInUnits(PressureUnits, 2);
            sheet.Cells[5 + startRow, 11].Value = ((1 - playerCarInfo.WheelsInfo.RearRight.TyreWear.ActualWear) * 100).ToString("F0");

            ExcelRange range = sheet.Cells[startRow + 1, 1, startRow + 5, 1];
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Merge = true;

            range = sheet.Cells[startRow + 1, 2, startRow + 1, 3];
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Merge = true;

            range = sheet.Cells[startRow + 1, 4, startRow + 1, 4];
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            range = sheet.Cells[startRow + 1, 7, startRow + 2, 8];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells[startRow + 1, 10, startRow + 2, 11];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells[startRow + 4, 7, startRow + 5, 8];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells[startRow + 4, 10, startRow + 5, 11];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells[startRow + 1, 1, startRow + 5, 11];
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick, System.Drawing.Color.Black);

            return startRow + 6;
        }

        private string GetBrakeTemperature(WheelInfo wheel)
        {
            return wheel.LeftTyreTemp.ActualQuantity.GetValueInUnits(TemperatureUnits, 0) + @"/"
                                                                           + wheel.CenterTyreTemp.ActualQuantity.GetValueInUnits(
                                                                               TemperatureUnits,
                                                                               0) + @"/" + wheel.RightTyreTemp.ActualQuantity
                                                                               .GetValueInUnits(TemperatureUnits, 0);
        }

        private void AddPlayerLapsHeader(ExcelWorksheet sheet)
        {
            sheet.Cells["A1"].Value = "Lap #";
            sheet.Cells["B1"].Value = "Lap Time";
            sheet.Cells["D1"].Value = "Best Δ";
            sheet.Cells["E1"].Value = "Fuel";
            sheet.Cells["G1"].Value = "LF Temp";
            sheet.Cells["H1"].Value = "Brake Temp";
            sheet.Cells["J1"].Value = "FR Temp";
            sheet.Cells["K1"].Value = "BrakeTemp";

            sheet.Cells["B2"].Value = "Sector 1";
            sheet.Cells["C2"].Value = "Sector 2";
            sheet.Cells["D2"].Value = "Sector 3";
            sheet.Cells["E2"].Value = "Fuel Change";
            sheet.Cells["G2"].Value = "LF Pressure";
            sheet.Cells["H2"].Value = "Tyre Condition";
            sheet.Cells["J2"].Value = "FR Pressure";
            sheet.Cells["K2"].Value = "Tyre Condition";

            sheet.Cells["B4"].Value = "Air Temp";
            sheet.Cells["C4"].Value = "Track Temp";
            sheet.Cells["D4"].Value = "Rain %";
            sheet.Cells["E4"].Value = "Water Temp";
            sheet.Cells["G4"].Value = "LR Temp";
            sheet.Cells["H4"].Value = "Brake Temp";
            sheet.Cells["J4"].Value = "RR Temp";
            sheet.Cells["K4"].Value = "BrakeTemp";

            sheet.Cells["E5"].Value = "Oil temp";
            sheet.Cells["G5"].Value = "LR Pressure";
            sheet.Cells["H5"].Value = "Tyre Condition";
            sheet.Cells["J5"].Value = "RR Pressure";
            sheet.Cells["K5"].Value = "Tyre Condition";

            sheet.Cells["M1"].Value = "UOM";
            sheet.Cells["M2"].Value = "Temperature: ";
            sheet.Cells["M3"].Value = "Pressure: ";
            sheet.Cells["M4"].Value = "Speed: ";
            sheet.Cells["M5"].Value = "Volume: ";
            sheet.Cells["N2"].Value = Temperature.GetUnitSymbol(TemperatureUnits);
            sheet.Cells["N3"].Value = Pressure.GetUnitSymbol(PressureUnits);
            sheet.Cells["N4"].Value = Velocity.GetUnitSymbol(VelocityUnits);
            sheet.Cells["N5"].Value = Volume.GetUnitSymbol(VolumeUnits);

            ExcelRange range = sheet.Cells["A1:A5"];
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Merge = true;

            range = sheet.Cells["B1:C1"];
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Merge = true;

            range = sheet.Cells["D1"];
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            range = sheet.Cells["G1:H2"];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells["J1:K2"];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells["G4:H5"];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells["J4:K5"];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);

            range = sheet.Cells["M1:N1"];
            range.Style.Font.Bold = true;
            range.Merge = true;

            range = sheet.Cells["A1:K5"];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick, System.Drawing.Color.Black);
            sheet.View.FreezePanes(7, 14);
        }

        private void AddDriverInfo(ExcelWorksheet sheet, ExcelRow row, Driver driver, SessionSummary sessionSummary)
        {
            sheet.Cells[row.Row + 1, 1].Value = GetFinishPositionInfo(driver, sessionSummary.SessionType == SessionType.Race);
            sheet.Cells[row.Row + 1, 2].Value = driver.DriverLongName;
            sheet.Cells[row.Row + 1, 3].Value = driver.CarName;
            sheet.Cells[row.Row + 1, 4].Value = driver.TotalLaps;
            sheet.Cells[row.Row + 1, 5].Value = driver.BestPersonalLap == null ? string.Empty : FormatTimeSpan(driver.BestPersonalLap.LapTime);
            sheet.Cells[row.Row + 1, 6].Value = driver.BestSector1Lap == null ? string.Empty : FormatTimeSpan(driver.BestSector1Lap.Sector1);
            sheet.Cells[row.Row + 1, 7].Value = driver.BestSector2Lap == null ? string.Empty : FormatTimeSpan(driver.BestSector2Lap.Sector2);
            sheet.Cells[row.Row + 1, 8].Value = driver.BestSector3Lap == null ? string.Empty : FormatTimeSpan(driver.BestSector3Lap.Sector3);
            sheet.Cells[row.Row + 1, 9].Value = driver.TopSpeed.GetValueInUnits(VelocityUnits).ToString("N0") + Velocity.GetUnitSymbol(VelocityUnits);

            var lapsWithTime = driver.Laps.Where(x => x.LapTime > TimeSpan.Zero).ToList();
            if (lapsWithTime.Count > 0 && driver.BestPersonalLap != null)
            {
                double totalSeconds = lapsWithTime.Select(x => x.LapTime.TotalSeconds).Sum();
                sheet.Cells[row.Row + 1, 11].Value = FormatTimeSpan(TimeSpan.FromSeconds(totalSeconds / lapsWithTime.Count));
                if (sessionSummary.SessionType == SessionType.Race)
                {
                    sheet.Cells[row.Row + 1, 12].Value = Velocity.FromMs(lapsWithTime.Count * sessionSummary.TrackInfo.LayoutLength.InMeters / totalSeconds).GetValueInUnits(VelocityUnits).ToString("F2") + Velocity.GetUnitSymbol(VelocityUnits);
                }
                else
                {
                    sheet.Cells[row.Row + 1, 12].Value = Velocity.FromMs(sessionSummary.TrackInfo.LayoutLength.InMeters / driver.BestPersonalLap.LapTime.TotalSeconds).GetValueInUnits(VelocityUnits).ToString("F2") + Velocity.GetUnitSymbol(VelocityUnits);
                }
            }

            if (IncludeRatings && driver.Rating != 0)
            {
                StringBuilder ratingInformation = new StringBuilder(driver.Rating.ToString("F0"));
                if (driver.RatingRelativeToPlayer != 0)
                {
                    ratingInformation.Append(" / ");
                    ratingInformation.Append(driver.RatingRelativeToPlayer.ToString("+#;-#;0"));
                }

                sheet.Cells[row.Row + 1, 13].Value = ratingInformation.ToString();

                sheet.Cells[row.Row + 1, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row.Row + 1, 13].Style.Font.Color.SetColor(System.Drawing.Color.White);
                if (driver.RatingRelativeToPlayer > 0)
                {
                    sheet.Cells[row.Row + 1, 13].Style.Fill.BackgroundColor.SetColor(RatingHigher.ToDrawingColor());
                }
                else
                {
                    sheet.Cells[row.Row + 1, 13].Style.Fill.BackgroundColor.SetColor(RatingLowerOrSame.ToDrawingColor());
                }
            }

            if (driver.FinishStatus is DriverFinishStatus.Dnf or DriverFinishStatus.Dq or DriverFinishStatus.Dnq)
            {
                sheet.Cells[row.Row + 1, 10].Value = driver.FinishStatus.ToString();
            }
            else if (driver.LapsDifferenceToPlayer != 0)
            {
                sheet.Cells[row.Row + 1, 10].Value = $"{driver.LapsDifferenceToPlayer:+#;-#;0} Lap";
            }
            else
            {
                if (sessionSummary.SessionType == SessionType.Race)
                {
                    sheet.Cells[row.Row + 1, 10].Style.Numberformat.Format = "+0.0; -0.0; 0.0";
                }
                else
                {
                    sheet.Cells[row.Row + 1, 10].Style.Numberformat.Format = "+0.000; -0.000; 0.000";
                }

                sheet.Cells[row.Row + 1, 10].Value = driver.GapToPlayerRelative.TotalSeconds;
            }

            if (driver.BestPersonalLap == sessionSummary.SessionBestLap)
            {
                sheet.Cells[row.Row + 1, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row.Row + 1, 5].Style.Fill.BackgroundColor.SetColor(SessionBestColor.ToDrawingColor());
                sheet.Cells[row.Row + 1, 5].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            if (driver.BestSector1Lap == sessionSummary.SessionBestSector1)
            {
                sheet.Cells[row.Row + 1, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row.Row + 1, 6].Style.Fill.BackgroundColor.SetColor(SessionBestColor.ToDrawingColor());
                sheet.Cells[row.Row + 1, 6].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            if (driver.BestSector2Lap == sessionSummary.SessionBestSector2)
            {
                sheet.Cells[row.Row + 1, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row.Row + 1, 7].Style.Fill.BackgroundColor.SetColor(SessionBestColor.ToDrawingColor());
                sheet.Cells[row.Row + 1, 7].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            if (driver.BestSector3Lap == sessionSummary.SessionBestSector3)
            {
                sheet.Cells[row.Row + 1, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[row.Row + 1, 8].Style.Fill.BackgroundColor.SetColor(SessionBestColor.ToDrawingColor());
                sheet.Cells[row.Row + 1, 8].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }
        }

        private void AddDriversInfoHeader(ExcelWorksheet sheet)
        {
            ExcelStyle style = sheet.Cells[5, 1, 5, 10].Style;
            style.Font.Bold = true;
            style.Font.Size = 14;
            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            sheet.Cells["A5"].Value = "#";
            sheet.Cells["B5"].Value = "Name";
            sheet.Cells["C5"].Value = "Car";
            sheet.Cells["D5"].Value = "Laps";
            sheet.Cells["E5"].Value = "Best Lap";
            sheet.Cells["F5"].Value = "Best S1";
            sheet.Cells["G5"].Value = "Best S2";
            sheet.Cells["H5"].Value = "Best S3";
            sheet.Cells["I5"].Value = "Top Speed";
            sheet.Cells["J5"].Value = "Gap";
            sheet.Cells["K5"].Value = "Avg Lap Time";
            sheet.Cells["L5"].Value = "Avg Speed";
            if (IncludeRatings)
            {
                sheet.Cells["M5"].Value = "Rating";
            }
        }

        private void AddTrackInformation(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            StringBuilder trackInformation = new StringBuilder(sessionSummary.SessionType.ToString());
            trackInformation.Append(" at: ");
            trackInformation.Append(sessionSummary.TrackInfo.TrackName);
            if (!string.IsNullOrWhiteSpace(sessionSummary.TrackInfo.TrackLayoutName))
            {
                trackInformation.Append(" (");
                trackInformation.Append(sessionSummary.TrackInfo.TrackLayoutName);
                trackInformation.Append(") (");
                trackInformation.Append(GetSessionLength(sessionSummary));
                trackInformation.Append(")");
            }

            sheet.Cells["A1"].Value = trackInformation.ToString();
            sheet.Cells[1, 1, 1, NumberOfSummaryColumns].Merge = true;
            ExcelStyle style = sheet.Cells["A1"].Style;

            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.PatternColor.SetColor(System.Drawing.Color.Black);
            style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Black);
            style.VerticalAlignment = ExcelVerticalAlignment.Center;
            style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            style.Font.Color.SetColor(System.Drawing.Color.White);
            style.Font.Bold = true;
            style.Font.Size = 18;

            sheet.Row(1).Height = 35;
        }

        private void AddSessionBestInfo(SessionSummary sessionSummary, ExcelWorksheet sheet)
        {
            ExcelRange range = sheet.Cells[5, SummaryInfoBoxesFirstColumn, 9, SummaryInfoBoxesFirstColumn + 1];
            range.Style.Border.BorderAround(ExcelBorderStyle.Medium, SessionBestColor.ToDrawingColor());
            sheet.Cells[5, SummaryInfoBoxesFirstColumn].Value = "Session Best:";
            sheet.Cells[6, SummaryInfoBoxesFirstColumn].Value = "Sector 1:";
            sheet.Cells[7, SummaryInfoBoxesFirstColumn].Value = "Sector 2:";
            sheet.Cells[8, SummaryInfoBoxesFirstColumn].Value = "Sector 3:";
            sheet.Cells[9, SummaryInfoBoxesFirstColumn].Value = "Lap:";

            if (sessionSummary.SessionBestSector1 != null)
            {
                sheet.Cells[6, SummaryInfoBoxesFirstColumn + 1].Value = FormatSessionBest(
                    sessionSummary.SessionBestSector1,
                    sessionSummary.SessionBestSector1.Sector1);
            }

            if (sessionSummary.SessionBestSector2 != null)
            {
                sheet.Cells[7, SummaryInfoBoxesFirstColumn + 1].Value = FormatSessionBest(
                    sessionSummary.SessionBestSector2,
                    sessionSummary.SessionBestSector2.Sector2);
            }

            if (sessionSummary.SessionBestSector3 != null)
            {
                sheet.Cells[8, SummaryInfoBoxesFirstColumn + 1].Value = FormatSessionBest(
                    sessionSummary.SessionBestSector3,
                    sessionSummary.SessionBestSector3.Sector3);
            }

            if (sessionSummary.SessionBestLap != null)
            {
                sheet.Cells[9, SummaryInfoBoxesFirstColumn + 1].Value = FormatSessionBest(
                    sessionSummary.SessionBestLap,
                    sessionSummary.SessionBestLap.LapTime);
            }

            sheet.Cells[5, SummaryInfoBoxesFirstColumn, 9, SummaryInfoBoxesFirstColumn].Style.Font.Bold = true;
            sheet.Cells[5, SummaryInfoBoxesFirstColumn + 1, 9, SummaryInfoBoxesFirstColumn + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            range.AutoFitColumns();
        }

        private void AddFuelConsumption(SessionSummary sessionSummary, Driver player, ExcelWorksheet sheet)
        {
            if (player == null || sessionSummary?.FuelConsumptionInformation == null || player.Laps.Count == 0)
            {
                return;
            }

            ExcelRange range = sheet.Cells[12, SummaryInfoBoxesFirstColumn, 17, SummaryInfoBoxesFirstColumn + 1];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);
            sheet.Cells[12, SummaryInfoBoxesFirstColumn].Value = "Initial Fuel:";
            sheet.Cells[13, SummaryInfoBoxesFirstColumn].Value = "Total Fuel Consumed:";
            sheet.Cells[14, SummaryInfoBoxesFirstColumn].Value = "Total Distance: ";
            sheet.Cells[15, SummaryInfoBoxesFirstColumn].Value = "Avg per Lap:";
            sheet.Cells[16, SummaryInfoBoxesFirstColumn].Value = "Avg per Minute:";
            sheet.Cells[17, SummaryInfoBoxesFirstColumn].Value = "Fuel Economy: ";

            if (player.Laps[0].LapStartSnapshot != null)
            {
                sheet.Cells[12, SummaryInfoBoxesFirstColumn + 1].Value = $"{player.Laps[0].LapStartSnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.GetValueInUnits(VolumeUnits):F2} {Volume.GetUnitSymbol(VolumeUnits)}";
            }

            sheet.Cells[13, SummaryInfoBoxesFirstColumn + 1].Value = $"{sessionSummary.FuelConsumptionInformation.ConsumedFuel.GetValueInUnits(VolumeUnits):F2} {Volume.GetUnitSymbol(VolumeUnits)}";
            sheet.Cells[14, SummaryInfoBoxesFirstColumn + 1].Value = $"{Distance.FromMeters(sessionSummary.FuelConsumptionInformation.TraveledDistanceMeters).GetByUnit(DistanceUnits):F2} {Distance.GetUnitsSymbol(DistanceUnits)}";
            sheet.Cells[15, SummaryInfoBoxesFirstColumn + 1].Value = $"{sessionSummary.FuelConsumptionInformation.GetAveragePerLap().GetValueInUnits(VolumeUnits):F2} {Volume.GetUnitSymbol(VolumeUnits)}";
            sheet.Cells[16, SummaryInfoBoxesFirstColumn + 1].Value = $"{sessionSummary.FuelConsumptionInformation.GetAveragePerMinute().GetValueInUnits(VolumeUnits):F2} {Volume.GetUnitSymbol(VolumeUnits)}";

            if (VolumeUnits == VolumeUnits.Liters)
            {
                sheet.Cells[17, SummaryInfoBoxesFirstColumn + 1].Value = $"{sessionSummary.FuelConsumptionInformation.Consumption.InVolumePer100Km.GetValueInUnits(VolumeUnits):F2} L/100Km";
            }
            else
            {
                sheet.Cells[17, SummaryInfoBoxesFirstColumn + 1].Value = $"{sessionSummary.FuelConsumptionInformation.Consumption.InDistancePerGallon.GetByUnit(DistanceUnits.Miles):F2} Mpg";
            }

            range = sheet.Cells[12, SummaryInfoBoxesFirstColumn, 17, SummaryInfoBoxesFirstColumn];
            range.AutoFitColumns();

            if (sessionSummary.FuelConsumptionInformation.HasVirtualEnergyFuelCoef)
            {
                AddVirtualEnergyConsumption(sessionSummary, player, sheet);
            }
        }

        private void AddVirtualEnergyConsumption(SessionSummary sessionSummary, Driver player, ExcelWorksheet sheet)
        {
            if (player == null || sessionSummary?.FuelConsumptionInformation == null || player.Laps.Count == 0)
            {
                return;
            }

            ExcelRange range = sheet.Cells[12, SummaryInfoBoxesFirstColumn + 2, 15, SummaryInfoBoxesFirstColumn + 3];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);
            sheet.Cells[12, SummaryInfoBoxesFirstColumn + 2].Value = "Initial VE:";
            sheet.Cells[13, SummaryInfoBoxesFirstColumn + 2].Value = "Total VE Used:";
            sheet.Cells[14, SummaryInfoBoxesFirstColumn + 2].Value = "Avg per Lap:";
            sheet.Cells[15, SummaryInfoBoxesFirstColumn + 2].Value = "Avg per Minute:";

            if (player.Laps[0].LapStartSnapshot != null)
            {
                sheet.Cells[12, SummaryInfoBoxesFirstColumn + 3].Value = $"{player.Laps[0].LapStartSnapshot.PlayerData.CarInfo.FuelSystemInfo.VirtualTank.InPercentage:F0} %";
            }

            double fuelToVirtualEnergyCoef = 1 / sessionSummary.FuelConsumptionInformation.VirtualEnergyFuelCoef;

            sheet.Cells[13, SummaryInfoBoxesFirstColumn + 3].Value = $"{sessionSummary.FuelConsumptionInformation.ConsumedFuel.InLiters * fuelToVirtualEnergyCoef:F2} %";
            sheet.Cells[14, SummaryInfoBoxesFirstColumn + 3].Value = $"{sessionSummary.FuelConsumptionInformation.GetAveragePerLap().InLiters * fuelToVirtualEnergyCoef:F2} %";
            sheet.Cells[15, SummaryInfoBoxesFirstColumn + 3].Value = $"{sessionSummary.FuelConsumptionInformation.GetAveragePerMinute().InLiters * fuelToVirtualEnergyCoef:F2} %";

            range = sheet.Cells[12, SummaryInfoBoxesFirstColumn + 2, 15, SummaryInfoBoxesFirstColumn + 3];
            range.AutoFitColumns();
        }

        private void AddPitsInfo(SessionSummary sessionSummary, ExcelWorksheet sheet)
        {
            int currentRow = 20;
            int pitStopIndex = 1;
            foreach (PitStopSummary pitStopSummary in sessionSummary.PitStops)
            {
                currentRow = AddPitStopInfo(pitStopIndex++, pitStopSummary, sheet, currentRow, sessionSummary.FuelConsumptionInformation);
                currentRow += 2;
            }
        }

        private int AddPitStopInfo(int pitNumber, PitStopSummary pitStopSummary, ExcelWorksheet sheet, int startRow, SessionFuelConsumptionDto fuelConsumptionInfo)
        {
            int currentRow = startRow;
            double fuelPerLap = fuelConsumptionInfo.GetAveragePerLap().InLiters;
            double vePerLap = fuelConsumptionInfo.HasVirtualEnergyFuelCoef ? fuelConsumptionInfo.GetAveragePerLap().InLiters / fuelConsumptionInfo.VirtualEnergyFuelCoef : 0;
            sheet.Cells[startRow, SummaryInfoBoxesFirstColumn].Value = $"Pit Stop {pitNumber}";
            sheet.Cells[startRow, SummaryInfoBoxesFirstColumn].Style.Font.Bold = true;
            currentRow++;

            sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Entry Lap: ";
            sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = pitStopSummary.EntryLapNumber.ToString();
            currentRow++;

            if (pitStopSummary.WasDriverThrough)
            {
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Penalty: ";
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"Drive Through";
                currentRow++;

                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Total Duration: ";
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.OverallDuration.TotalSeconds:F1}s";
                currentRow++;
            }
            else if (pitStopSummary.IsFrontTyresChanged == false && pitStopSummary.IsRearTyresChanged == false && pitStopSummary.FuelTaken.InLiters <= 0)
            {
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Penalty: ";
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"Stop & Go";
                currentRow++;

                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Total Duration: ";
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.OverallDuration.TotalSeconds:F1}s";
                currentRow++;
            }
            else
            {
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Full Duration: ";
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.OverallDuration.TotalSeconds:F1}s";
                currentRow++;

                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Stopped For: ";
                sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.StallDuration.TotalSeconds:F1}s";
                currentRow++;

                if (pitStopSummary.VirtualEnergyTaken.InPercentage > 0 && vePerLap > 0)
                {
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"VE: ";
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.VirtualEnergyTaken.InPercentage:F0}% ({pitStopSummary.VirtualEnergyTaken.InPercentage / vePerLap:F0} Laps)";
                    currentRow++;
                }

                if (pitStopSummary.AfterStopVirtualEnergy.InPercentage > 0 && vePerLap > 0)
                {
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Total VE: ";
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.AfterStopVirtualEnergy.InPercentage:F0}% ({pitStopSummary.AfterStopVirtualEnergy.InPercentage / vePerLap:F0} Laps)";
                    currentRow++;
                }

                if (pitStopSummary.FuelTaken.InLiters > 0)
                {
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Fuel: ";
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.FuelTaken.GetValueInUnits(VolumeUnits):F2} {Volume.GetUnitSymbol(VolumeUnits)} ({pitStopSummary.FuelTaken.InLiters / fuelPerLap:F0} Laps)";
                    currentRow++;
                }

                if (pitStopSummary.AfterStopFuel.InLiters > 0)
                {
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = $"Total Fuel: ";
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = $"{pitStopSummary.AfterStopFuel.GetValueInUnits(VolumeUnits):F2} {Volume.GetUnitSymbol(VolumeUnits)} ({pitStopSummary.AfterStopFuel.InLiters / fuelPerLap:F0} Laps)";
                    currentRow++;
                }

                if (pitStopSummary.IsFrontTyresChanged)
                {
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = "New Front Tyres: ";
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = pitStopSummary.NewFrontCompound;
                    currentRow++;
                }

                if (pitStopSummary.IsRearTyresChanged)
                {
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn].Value = "New Rear Tyres: ";
                    sheet.Cells[currentRow, SummaryInfoBoxesFirstColumn + 1].Value = pitStopSummary.NewRearCompound;
                    currentRow++;
                }
            }

            ExcelRange range = sheet.Cells[startRow, SummaryInfoBoxesFirstColumn, currentRow - 1, SummaryInfoBoxesFirstColumn + 1];
            range.Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.Black);
            return currentRow;
        }

        private string FormatSessionBest(Lap lap, TimeSpan timeSpan)
        {
            if (lap == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(lap.Driver.DriverLongName);
            sb.Append("-Lap: ");
            sb.Append(lap.LapNumber);
            sb.Append(" | ");
            sb.Append(FormatTimeSpan(timeSpan));
            return sb.ToString();
        }

        private string GetSessionLength(SessionSummary sessionSummary)
        {
            if (sessionSummary.SessionLengthType == SessionLengthType.Laps)
            {
                return sessionSummary.TotalNumberOfLaps + " Laps";
            }

            if (sessionSummary.SessionLength.Hours > 0)
            {
                return sessionSummary.SessionLength.Hours + "h " + (sessionSummary.SessionLength.Minutes + 1) + "min";
            }

            return Math.Ceiling((decimal)sessionSummary.SessionLength.Minutes) + "mins";
        }

        private void AddTyresDegradation(ExcelWorksheet sheet, SessionSummary sessionSummary)
        {
            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (player == null)
            {
                return;
            }

            int totalLaps = player.TotalLaps;
            int startRow = 100;
            int startColumn = 1;
            sheet.Cells[startRow + 1, startColumn].Value = "Start";
            sheet.Cells[startRow, startColumn + 1].Value = "FL";
            sheet.Cells[startRow, startColumn + 2].Value = "FR";
            sheet.Cells[startRow, startColumn + 3].Value = "RF";
            sheet.Cells[startRow, startColumn + 4].Value = "RR";
            GenerateNumberColumn(sheet, new ExcelCellAddress(startRow + 2, startColumn), totalLaps);
            GenerateTyresConditionsRows(sheet, new ExcelCellAddress(startRow + 1, startColumn + 1), player);

            ExcelLineChart chart = (ExcelLineChart)sheet.Drawings.AddChart("Tyres Condition", eChartType.LineMarkers);
            chart.SetPosition(0, 0, 0, 0);
            int currentColumn = 2;

            ExcelLineChartSerie series = (ExcelLineChartSerie)chart.Series.Add(ExcelCellBase.GetAddress(startRow + 1, currentColumn, startRow + 1 + totalLaps, currentColumn), ExcelCellBase.GetAddress(startRow + 1, 1, startRow + 1 + totalLaps, 1));
            series.Header = "Front Left";
            currentColumn++;

            series = (ExcelLineChartSerie)chart.Series.Add(ExcelCellBase.GetAddress(startRow + 1, currentColumn, startRow + 1 + totalLaps, currentColumn), ExcelCellBase.GetAddress(startRow + 1, 1, startRow + 1 + totalLaps, 1));
            series.Header = "Front Right";
            currentColumn++;

            series = (ExcelLineChartSerie)chart.Series.Add(ExcelCellBase.GetAddress(startRow + 1, currentColumn, startRow + 1 + totalLaps, currentColumn), ExcelCellBase.GetAddress(startRow + 1, 1, startRow + 1 + totalLaps, 1));
            series.Header = "Rear Left";
            currentColumn++;

            series = (ExcelLineChartSerie)chart.Series.Add(ExcelCellBase.GetAddress(startRow + 1, currentColumn, startRow + 1 + totalLaps, currentColumn), ExcelCellBase.GetAddress(startRow + 1, 1, startRow + 1 + totalLaps, 1));
            series.Header = "Rear Right";

            chart.ShowDataLabelsOverMaximum = false;
            chart.DataLabel.ShowValue = true;
            chart.ShowHiddenData = true;

            chart.Axis[1].MinValue = 0;
            chart.Axis[1].TickLabelPosition = eTickLabelPosition.NextTo;
            chart.Axis[1].MajorUnit = 25;
            chart.Axis[1].MajorGridlines.LineStyle = eLineStyle.Dash;
            chart.Axis[1].MinorGridlines.LineStyle = eLineStyle.Dot;
            chart.Axis[1].MinorUnit = 5;
            chart.Axis[1].Orientation = eAxisOrientation.MinMax;
            chart.Axis[1].MaxValue = 100;
            chart.SetSize(70 * totalLaps, 800);
            chart.Axis[0].MajorUnit = 1;
            chart.Axis[0].MinorUnit = 1;
            chart.Title.Text = "Tyres Condition";
        }

        private void GenerateTyresConditionsRows(ExcelWorksheet sheet, ExcelCellAddress cellAddress, Driver player)
        {
            if (player.Laps.Count == 0)
            {
                return;
            }
            
            foreach (Lap playerLap in player.Laps)
            {
                if (playerLap.LapStartSnapshot?.PlayerData?.CarInfo?.WheelsInfo == null)
                {
                    cellAddress = new ExcelCellAddress(cellAddress.Row + 1, cellAddress.Column);
                    continue;
                }

                AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column, (1 - playerLap.LapStartSnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear) * 100);
                AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column + 1, (1 - playerLap.LapStartSnapshot.PlayerData.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear) * 100);
                AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column + 2, (1 - playerLap.LapStartSnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear) * 100);
                AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column + 3, (1 - playerLap.LapStartSnapshot.PlayerData.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear) * 100);
                cellAddress = new ExcelCellAddress(cellAddress.Row + 1, cellAddress.Column);
            }

            Lap lastLap = player.Laps.Last();
            if (lastLap.LapEndSnapshot?.PlayerData?.CarInfo?.WheelsInfo == null)
            {
                return;
            }

            AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column, (1 - lastLap.LapEndSnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear) * 100);
            AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column + 1, (1 - lastLap.LapEndSnapshot.PlayerData.CarInfo.WheelsInfo.FrontRight.TyreWear.ActualWear) * 100);
            AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column + 2, (1 - lastLap.LapEndSnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear) * 100);
            AddTyreDegradation(sheet, cellAddress.Row, cellAddress.Column + 3, (1 - lastLap.LapEndSnapshot.PlayerData.CarInfo.WheelsInfo.RearRight.TyreWear.ActualWear) * 100);
        }

        private void AddTyreDegradation(ExcelWorksheet sheet, int row, int column, double value)
        {
            sheet.Cells[row, column].Value = value;
            sheet.Cells[row, column].Style.Numberformat.Format = "0.00";
        }

        private void GenerateLapTimesRows(ExcelWorksheet sheet, ExcelCellAddress cellAddress, List<Lap> laps)
        {
            foreach (Lap playerLap in laps)
            {
                sheet.Cells[cellAddress.Row, cellAddress.Column].Value = playerLap.LapTime.TotalSeconds;
                cellAddress = new ExcelCellAddress(cellAddress.Row + 1, cellAddress.Column);
            }
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                return "-";
            }

            // String seconds = timeSpan.Seconds < 10 ? "0" + timeSpan.Seconds : timeSpan.Seconds.ToString();
            // String miliseconds = timeSpan.Milliseconds < 10 ? "0" + timeSpan.Seconds : timeSpan.Seconds.ToString();
            // return timeSpan.Minutes + ":" + timeSpan.Seconds + "." + timeSpan.Milliseconds;
            return timeSpan.ToString("mm\\:ss\\.fff");
        }
    }
}