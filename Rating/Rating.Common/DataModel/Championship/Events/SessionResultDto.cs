namespace SecondMonitor.Rating.Common.DataModel.Championship.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class SessionResultDto : IHaveManufacturersResults, IHaveTeamResults
    {
        public SessionResultDto()
        {
            DriverSessionResult = new List<DriverSessionResultDto>();
            ManufacturersSessionResult = new List<ManufacturerSessionResultDto>();
            ClassSessionResults = new List<ClassSessionResultDto>();
            TeamSessionResult = new List<TeamSessionResultDto>();
            ResultCreationTime = DateTime.UtcNow;
            Remarks = string.Empty;
            CompletionPercentage = 1.0;
        }

        public List<DriverSessionResultDto> DriverSessionResult { get; set; }

        public List<ManufacturerSessionResultDto> ManufacturersSessionResult { get; set; }

        public List<TeamSessionResultDto> TeamSessionResult { get; set; }

        public List<ClassSessionResultDto> ClassSessionResults { get; set; }

        [XmlIgnore]
        public bool HasManufacturersResults => ManufacturersSessionResult.Count > 0;

        [XmlAttribute]
        public DateTime ResultCreationTime { get; set; }

        [XmlAttribute]
        public string Remarks { get; set; }

        /// <summary>
        /// <remarks>0 - 1 Range</remarks>
        /// </summary>
        [XmlAttribute]
        public double CompletionPercentage { get; set; }

        public void AddRemark(string remark)
        {
            if (!string.IsNullOrWhiteSpace(Remarks))
            {
                Remarks += "\n";
            }

            Remarks += remark;
        }

        public ClassSessionResultDto GetOrCreateClassSessionResult(ClassIdentifier classIdentifier)
        {
            ClassSessionResultDto classSessionResult = ClassSessionResults.FirstOrDefault(x => x.ClassId == classIdentifier.ClassId && x.ClassName == classIdentifier.ClassName);
            if (classSessionResult != null)
            {
                return classSessionResult;
            }

            classSessionResult = new ClassSessionResultDto { ClassName = classIdentifier.ClassName, ClassId = classIdentifier.ClassId };
            ClassSessionResults.Add(classSessionResult);
            return classSessionResult;
        }

        public List<DriverSessionResultDto> GetDriverSessionResultForClass(ClassIdentifier classIdentifier)
        {
            return string.IsNullOrWhiteSpace(classIdentifier.ClassId) ? DriverSessionResult : DriverSessionResult.Where(x => x.ClassId == classIdentifier.ClassId).ToList();
        }

        public List<TeamSessionResultDto> GetTeamResults(ClassIdentifier classIdentifier)
        {
            return string.IsNullOrWhiteSpace(classIdentifier.ClassId) || ClassSessionResults.Count == 0 ? TeamSessionResult :
                ClassSessionResults.FirstOrDefault(x => x.ClassId == classIdentifier.ClassId)?.TeamSessionResult ?? [];
        }

        public List<ManufacturerSessionResultDto> GetManufacturerSessionResultDtos(string classId)
        {
            return string.IsNullOrWhiteSpace(classId) || ClassSessionResults.Count == 0 ? ManufacturersSessionResult :
                ClassSessionResults.FirstOrDefault(x => x.ClassId == classId)?.ManufacturersSessionResult ?? [];
        }
    }
}