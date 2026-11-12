namespace SecondMonitor.DataModel.DriversPresentation
{
    using System;
    using BasicProperties;

    [Serializable]
    public sealed class DriverPresentationDto
    {
        public DriverPresentationDto()
        {
        }

        public DriverPresentationDto(string driverName, bool customOutLineEnabled, ColorDto outLineColor)
        {
            DriverName = driverName;
            CustomOutLineEnabled = customOutLineEnabled;
            OutLineColor = outLineColor;
        }

        public string DriverName { get; set; }
        public bool CustomOutLineEnabled { get; set; }
        public ColorDto OutLineColor { get; set; }
    }
}