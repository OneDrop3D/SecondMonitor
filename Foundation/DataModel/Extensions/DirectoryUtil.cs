namespace SecondMonitor.DataModel.Extensions
{
    using System;
    using System.IO;

    public static class DirectoryUtil
    {
        public static string MyDocumentsPlaceHolder = "%MyDocuments%";
        public static string MyGamesPlaceHolder = "%MyGames%";

        public static string PopulateSpecialDirPlaceholders(string directory)
        {
            return directory.Replace(MyDocumentsPlaceHolder, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                .Replace(MyGamesPlaceHolder, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games"));
        }
    }
}
