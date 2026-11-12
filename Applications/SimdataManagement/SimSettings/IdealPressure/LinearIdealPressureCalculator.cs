namespace SecondMonitor.SimdataManagement.SimSettings.IdealPressure
{
    using System;
    using DataModel.Snapshot.Systems;

    public class LinearIdealPressureCalculator : IIdealPressureByTemperatureCalculator
    {
        private const double LinearSlope = 2.2;
        public void FillIdealPressure(WheelInfo wheelInfo)
        {
            double currentPressure = wheelInfo.TyrePressure.ActualQuantity.InKpa;
            double idealMidTemperature = (wheelInfo.LeftTyreTemp.ActualQuantity.InKelvin + wheelInfo.RightTyreTemp.ActualQuantity.InKelvin) / 2.0;
            double actualMidTemperature = wheelInfo.CenterTyreTemp.ActualQuantity.InKelvin;
            double pressureChange = currentPressure - (currentPressure * (actualMidTemperature) / idealMidTemperature);

            double differenceInTemperature = Math.Abs(wheelInfo.LeftTyreTemp.ActualQuantity.InKelvin - wheelInfo.RightTyreTemp.ActualQuantity.InKelvin);
            double idealCamberCoef = differenceInTemperature / 10;

            wheelInfo.TyrePressure.IdealQuantity.InKpa = currentPressure + (pressureChange * LinearSlope);
            wheelInfo.TyrePressure.IdealQuantityWindow.InKpa = Math.Max(2 * idealCamberCoef, 0.2);
        }
    }
}