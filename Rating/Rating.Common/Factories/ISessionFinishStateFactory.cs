namespace SecondMonitor.Rating.Common.Factories
{
    using DataModel;
    using SecondMonitor.DataModel.Summary;

    public interface ISessionFinishStateFactory
    {
        SessionFinishState Create(SessionSummary sessionSummary);
    }
}