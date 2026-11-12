namespace SecondMonitor.DataModel.DriversPresentation
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public sealed class DriverPresentationsDto
    {
        public DriverPresentationsDto()
        {
            DriverPresentations = new List<DriverPresentationDto>();
        }

        public List<DriverPresentationDto> DriverPresentations { get; set; }
    }
}