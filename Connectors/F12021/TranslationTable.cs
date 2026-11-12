namespace SecondMonitor.F12021Connector
{
    using System.Linq;

    internal static class TranslationTable
    {
        private static readonly string[] Tracks = new string[]
        {
            "Melbourne Grand Prix Circuit",
            "Circuit Paul Ricard",
            "Shanghai International Circuit",
            "Bahrain International Circuit",
            "Circuit de Barcelona-Catalunya",
            "Circuit de Monaco",
            "Circuit Gilles Villeneuve",
            "Silverstone",
            "Hockenheimring",
            "Hungaroring",
            "Circuit de Spa-Francorchamps",
            "Autodromo Nazionale Monza",
            "Marina Bay Street Circuit",
            "Suzuka Circuit",
            "Yas Marina Circuit",
            "Circuit of the Americas",
            "Autódromo José Carlos Pace",
            "Red Bull Ring",
            "Sochi Autodrom",
            "Autódromo Hermanos Rodríguez",
            "Baku City Circuit",
            "Bahrain International Circuit - Short",
            "Silverstone Short",
            "Circuit of the Americas Short",
            "Suzuka Short",
            "Hanoi Circuit",
            "Zandvoort",
            "Imola",
            "Algarve International Circuit",
            "Jeddah Street Circuit",
        };

        private static readonly string[] Cars = new string[]
        {
            "Mercedes AMG",
            "Ferrari",
            "Red Bull Racing",
            "Williams",
            "Aston Martin",
            "Alpine",
            "Alpha Tauri",
            "Haas",
            "McLaren",
            "Alfa Romeo",
            "McLaren MP4/4 (1988)",
            "McLaren MP4/6 (1991)",
            "Williams FW14 (1992)",
            "Ferrari 412 T2 (1995)",
            "Williams FW18 (1996)",
            "McLaren MP4/13 (1998)",
            "Ferrari F2002 (2002)",
            "Ferrari F2004 (2004)",
            "Renault R26 (2006)",
            "Ferrari F2007 (2007)",
            "McLaren MP4-23 (2008)",
            "Red Bull RB6 (2010)",
            "Ferrari 312T (1976)",
            "ART Grand Prix",
            "Campos Vexatec Racing",
            "Carlin",
            "Charouz Racing System",
            "DAMS",
            "Russian Time",
            "MP Motorsport",
            "Pertamina",
            "McLaren MP4/5 (1990)",
            "Trident",
            "BWT Arden",
            "McLaren M23 (1976)",
            "Lotus 72 (1972)",
            "Ferrari 312T4 (1979)",
            "McLaren MP4/1 (1982)",
            "Williams FW25 (2003)",
            "Brawn BGP 001 (2009)",
            "Lotus 78 (1978)",
            "F1 Generic",
            "ART Grand Prix",
            "Campos Vexatec Racing",
            "Carlin",
            "Sauber Junior Charouz",
            "DAMS",
            "Uni-Virtuosi",
            "MP Motorsport",
            "Prema",
            "Trident",
            "Arden",
            string.Empty,
            "Benetton B194 (1994)",
            "Benetton B195  (1995)",
            "Ferrari F1-2000 (2000)",
            "Jordan 191 (1991)",
        }.Concat(Enumerable.Repeat<string>(string.Empty, 13)).Concat(new string[]
        {
            "ART Grand Prix",
            "Campos Racing",
            "Carlin",
            "Charouz",
            "DAMS",
            "Uni-Virtuosi",
            "MP Motorsport",
            "Prema",
            "Trident",
            "BWT",
            "Hitech ",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "Mercedes AMG",
            "Ferrari",
            "Red Bull Racing",
            "Williams",
            "Racing Point",
            "Renault",
            "Alpha Tauri",
            "Haas",
            "McLaren",
            "Alfa Romeo",
        }).ToArray();

        private static readonly string[] Classes = new string[]
        {
            "Formula 1", "F1 Classic", "Formula 2", "Formula 1 Generic"
        };

        internal static string GetTrackName(int index) => TranslateSafe(index, Tracks);

        internal static string GetCarName(int index) => TranslateSafe(index, Cars);

        internal static string GetClass(int index) => TranslateSafe(index, Classes);

        internal static string GetTyreCompound(int tyreType)
        {
            switch (tyreType)
            {
                case 7:
                    return "Intermediate";
                case 10:
                case 15:
                case 8:
                    return "Wet";
                case 9:
                    return "Prime";
                case 11:
                    return "SuperSoft";
                case 12:
                    return "Soft";
                case 13:
                    return "Medium";
                case 14:
                    return "Hard";
                case 16:
                    return "Soft";
                case 17:
                    return "Medium";
                case 18:
                    return "Hard";
                default:
                    return string.Empty;
            }
        }

        private static string TranslateSafe(int index, string[] translations)
        {
            return index >= translations.Length ? "Unknown" : translations[index];
        }

        internal static string GetTyreVisualCompound(int tyreType)
        {
            switch (tyreType)
            {
                case 7:
                    return "Intermediate";
                case 10:
                case 15:
                case 8:
                    return "Wet";
                case 9:
                    return "Prime";
                case 11:
                    return "SuperSoft";
                case 12:
                    return "Soft";
                case 13:
                    return "Medium";
                case 14:
                    return "Hard";
                case 16:
                    return "Soft";
                case 17:
                    return "Medium";
                case 18:
                    return "Hard";
                default:
                    return string.Empty;
            }
        }
    }
}