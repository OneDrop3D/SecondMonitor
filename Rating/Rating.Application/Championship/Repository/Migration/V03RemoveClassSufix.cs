namespace SecondMonitor.Rating.Application.Championship.Repository.Migration
{
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels.Repository;

    public class V03RemoveClassSuffix : IDtoMigration<AllChampionshipsDto>
    {
        public int VersionAfterMigration => 3;

        public AllChampionshipsDto ApplyMigration(AllChampionshipsDto oldVersion)
        {
            oldVersion.Championships.ForEach(x => MigrateUp(x));
            return oldVersion;
        }

        private void MigrateUp(ChampionshipDto championshipDto)
        {
            championshipDto.Drivers.ForEach(x => {
                x.LastClassName = RemoveClassSuffixes(x.LastClassName);
                x.LastClassId = RemoveClassSuffixes(x.LastClassId);
            });

            foreach (SessionResultDto sessionResultDto in championshipDto.GetAllResults())
            {
                sessionResultDto.DriverSessionResult.ForEach(x => {
                    x.ClassId = RemoveClassSuffixes(x.ClassId);
                    x.ClassName = RemoveClassSuffixes(x.ClassId);
                });

                foreach (ClassSessionResultDto classSessionResultDto in sessionResultDto.ClassSessionResults)
                {
                    classSessionResultDto.ClassId = RemoveClassSuffixes(classSessionResultDto.ClassId);
                    classSessionResultDto.ClassName = RemoveClassSuffixes(classSessionResultDto.ClassName);
                }
            }
        }

        private static string RemoveClassSuffixes(string classIdentifier)
        {
            if(string.IsNullOrWhiteSpace(classIdentifier))
            {
                return string.Empty;
            }

            classIdentifier = classIdentifier.EndsWith("_LD") ?
                classIdentifier.Replace("_LD", string.Empty)
                : classIdentifier;

            classIdentifier = classIdentifier.EndsWith("_HD") ?
                classIdentifier.Replace("_HD", string.Empty)
                : classIdentifier;

            return classIdentifier;
        }
    }
}
