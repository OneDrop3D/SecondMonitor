namespace SecondMonitor.Rating.Application.Championship.Filters
{
    public class ConditionResult
    {
        public ConditionResult(RequirementResultKind result) : this(result, string.Empty)
        {
        }

        public ConditionResult(RequirementResultKind result, string notes)
        {
            Result = result;
            Notes = notes;
        }

        public RequirementResultKind Result { get; }

        public string Notes { get; }
    }
}