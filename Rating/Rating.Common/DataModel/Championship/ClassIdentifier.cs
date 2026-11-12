namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    public class ClassIdentifier
    {
        private static readonly string[] classSuffixes = ["_LD", "_HD"];
        public static ClassIdentifier OverviewIdentifier { get; } = new ClassIdentifier(string.Empty, string.Empty);

        public ClassIdentifier(string className, string classId)
        {
            ClassName = RemoveClassSuffixes(className);
            ClassId = RemoveClassSuffixes(classId);
        }

        public string ClassName { get; }
        public string ClassId { get; }

        public static string RemoveClassSuffixes(string classIdentifier)
        {
            foreach (string classSuffix in classSuffixes)
            {
                if (classIdentifier.EndsWith(classSuffix))
                {
                    classIdentifier = classIdentifier.Replace(classSuffix, string.Empty);
                }
            }

            return classIdentifier;
        }
    }
}
