namespace SecondMonitor.DataModel.BasicProperties.Units
{
    public enum TorqueUnits
    {
        Nm,
        //This would break stored values
#pragma warning disable SA1300
        lbf
#pragma warning restore SA1300
    }
}