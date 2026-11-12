namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults
{
    using System;

    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class AmendedDriverResult
    {
        public AmendedDriverResult(int amendedDriverFinishPosition, int amendedClassFinishPosItion, DriverSessionResultDto originalDriverResult)
        {
            AmendedDriverFinishPosition = amendedDriverFinishPosition;
            AmendedDriverClassFinishPosition = amendedClassFinishPosItion;
            OriginalDriverResult = originalDriverResult;
        }

        public DriverSessionResultDto OriginalDriverResult { get; }

        public Guid DriverGuid => OriginalDriverResult.DriverGuid;
        public string DriverName => OriginalDriverResult.DriverName;
        public int AmendedDriverFinishPosition { get; }
        public int AmendedDriverClassFinishPosition { get; }
        public string CarName => OriginalDriverResult.CarName;

        public string ClassId => OriginalDriverResult.ClassId;
        public string ClassName => OriginalDriverResult.ClassName;
        public string TeamName => OriginalDriverResult.TeamName;
        public bool WasFastestLap => OriginalDriverResult.WasFastestLap;
    }
}
