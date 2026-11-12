namespace SecondMonitor.Rating.Application.Championship.Manufacturers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using NLog;

    public static class AllManufacturers
    {
        private const string ManufacturersFile = "Manufacturers.txt";
        private static readonly List<Manufacturer> manufacturersInternal = new List<Manufacturer>();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static AllManufacturers()
        {
            try
            {
                using StreamReader streamReader = new(Path.Combine(AssemblyDirectory, ManufacturersFile));
                while (streamReader.Peek() >= 0)
                {
                    string line = streamReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        break;
                    }

                    string[] splits = line.Split(';');
                    manufacturersInternal.Add(splits.Length == 1 ? new Manufacturer(splits[0]) : new Manufacturer(splits[0], splits.Skip(1).ToArray()));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to read manufacturers");
            }
        }

        public static IReadOnlyList<Manufacturer> Manufacturers => manufacturersInternal.AsReadOnly();

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}