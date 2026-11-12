namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System.Collections.Generic;

    public class ClassSessionResultDto : IHaveManufacturersResults, IHaveTeamResults
    {
        public ClassSessionResultDto()
        {
            ClassSessionResult = new List<ClassDriverResultDto>();
            ManufacturersSessionResult = new List<ManufacturerSessionResultDto>();
            TeamSessionResult = new List<TeamSessionResultDto>();
        }

        public string ClassName { get; set; }

        public string ClassId { get; set; }

        public List<ClassDriverResultDto> ClassSessionResult { get; set; }
        public List<ManufacturerSessionResultDto> ManufacturersSessionResult { get; set; }
        public List<TeamSessionResultDto> TeamSessionResult { get; set; }

        public void Clear()
        {
            ClassSessionResult.Clear();
            ManufacturersSessionResult.Clear();
            TeamSessionResult.Clear();
        }

        public ClassIdentifier GetClassIdentifier()
        {
            return new ClassIdentifier(ClassName, ClassId);
        }
    }
}
