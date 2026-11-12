namespace SecondMonitor.DataModel.Summary.FuelStrategy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class FuelStrategies
    {
        public List<ClassFuelStrategy> ClassFuelStrategies { get; set; } = new List<ClassFuelStrategy>();

        public ClassFuelStrategy GetOrCreateFuelStrategy(string simName, string classId)
        {
            return ClassFuelStrategies.SingleOrDefault(x => x.SimName == simName && x.ClassId == classId) ?? new ClassFuelStrategy()
            {
                SimName = simName,
                ClassId = classId,
            };
        }

        public void UpdateFuelStrategy(ClassFuelStrategy classFuelStrategy)
        {
            ClassFuelStrategies.RemoveAll(x => x.ClassId == classFuelStrategy.ClassId && x.SimName == classFuelStrategy.SimName);
            ClassFuelStrategies.Add(classFuelStrategy);
        }
    }
}
