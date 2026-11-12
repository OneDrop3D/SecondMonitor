namespace SecondMonitor.Contracts.SimSettings
{
    using System;
    using DataModel.OperationalRange;

    public interface ICarSpecificationProvider
    {
        event EventHandler<EventArgs> DataSourcePropertiesUpdated;

        void SaveAllProperties();

        void SaveDataSourceProperties(DataSourceProperties dataSourceProperties);

        DataSourceProperties GetSimulatorProperties(string simulatorName);

        CarModelProperties GetCarModelProperties(string simulatorName, string carProperties);
    }
}