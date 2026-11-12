namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System.Collections.Generic;

    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public interface IHaveManufacturersResults
    {
        public List<ManufacturerSessionResultDto> ManufacturersSessionResult { get; }
    }
}
