namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults
{
    using System.Collections.Generic;

    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class AmendedSessionResult
    {
        public AmendedSessionResult(SessionResultDto originalResuts, IReadOnlyList<AmendedDriverResult> amendedDriverResults)
        {
            OriginalResuts = originalResuts;
            AmendedDriverResults = amendedDriverResults;
        }

        public SessionResultDto OriginalResuts { get; }
        
        public IReadOnlyList<AmendedDriverResult> AmendedDriverResults { get; }
    }
}
