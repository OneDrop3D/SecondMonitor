namespace SecondMonitor.F12024Connector
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class TranslationTable
    {
        private static readonly Dictionary<string, string> Drivers = new Dictionary<string, string>()
        {
            { "VERSTAPPEN", "Max Verstappen" },
            { "NORRIS", "Lando Norris" },
            { "LECLERC", "Charles Leclerc" },
            { "PIASTRI", "Oscar Piastri" },
            { "SAINZ", "Carlos Sainz Jr" },
            { "RUSSELL", "George Russell" },
            { "HAMILTON", "Lewis Hamilton" },
            { "PÉREZ", "Sergio Pérez" },
            { "ALONSO", "Fernando Alonso" },
            { "GASLY", "Pierre Gasly" },
            { "HULKENBERG", "Nico Hülkenberg" },
            { "TSUNODA", "Yuki Tsunoda" },
            { "STROLL", "Lance Stroll" },
            { "OCON", "Esteban Ocon" },
            { "MAGNUSSEN", "Kevin Magnussen" },
            { "ALBON", "Alexander Albon" },
            { "RICCIARDO", "Daniel Ricciardo" },
            { "BEARMAN", "Oliver Bearman" },
            { "COLAPINTO", "Franco Colapinto" },
            { "ZHOU", "Zhou Guanyu" },
            { "LAWSON", "Liam Lawson" },
            { "SARGEANT", "Logan Sargeant" },
            { "DOOHAN", "Jack Doohan" },
            { "BOTTAS", "Valtteri Bottas" },
        };

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
            "Miami International Autodrome",
            "Las Vegas Strip Circuit",
            "Lusail International Circuit",
        };

        private static readonly string[] Cars = new string[]
            {
                "Mercedes-AMG F1 W15",
                "Ferrari SF-24",
                "Red Bull Racing RB20",
                "Williams FW46",
                "Aston Martin AMR24",
                "Alpine A524",
                "RB VCARB 01",
                "Haas VF-24",
                "McLaren MCL38",
                "Kick Sauber C44"
            }
            .Concat(Enumerable.Repeat<string>(string.Empty, 31))
            .Concat([
                "F1 Generic",
            ])
            .Concat(Enumerable.Repeat<string>(string.Empty, 62))
            .Concat([
                "F1 Custom Team",
            ])
            .Concat(Enumerable.Repeat<string>(string.Empty, 38))
            .Concat(Enumerable.Repeat<string>("Dallara F2", 30)).ToArray();

        private static readonly string[] Teams = new string[]
            {
                "Mercedes",
                "Ferrari",
                "Red Bull Racing",
                "Williams",
                "Aston Martin",
                "Alpine",
                "RB VCARB",
                "Haas",
                "McLaren",
                "Kick Sauber"
            }
            .Concat(Enumerable.Repeat<string>(string.Empty, 31))
            .Concat([
                "F1 Generic",
            ])
            .Concat(Enumerable.Repeat<string>(string.Empty, 62))
            .Concat([
                "F1 Custom Team",
            ])
            .Concat(Enumerable.Repeat<string>(string.Empty, 38))
            .Concat([
                "Art GP",
                "Campos Racing",
                "Rodin Carlin",
                "PHM Racing",
                "Dams",
                "Hitech Grand Prix",
                "MP Motorsport ‘23",
                "Prema Racing",
                "Trident",
                "Van Amersfoort Racing",
                "Virtuosi"
                
            ]).Concat(Enumerable.Repeat<string>(string.Empty, 4))
            .Concat([
                "Art GP",
                "Campos Racing",
                "Rodin Motosport",
                "AIX Racing",
                "Dams Lucas Oil",
                "Hitech Pulse-Eight",
                "MP Motorsport",
                "Prema Racing",
                "Trident",
                "Van Amersfoort Racing",
                "Invicta Racing"
            ])
            .ToArray();

        internal static string GetTrackName(int index) => TranslateSafe(index, Tracks);

        internal static string GetCarName(int index) => TranslateSafe(index, Cars);

        public static string GetTeamName(int index) => TranslateSafe(index, Teams);

        internal static string GetDriverName(string simDriverName) => Drivers.GetValueOrDefault(simDriverName, simDriverName);

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
            return index >= translations.Length ? $"Unknown ID: {index}" : translations[index];
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
                case 20:
                    return "Soft";
                case 17:
                case 21:
                    return "Medium";
                case 18:
                case 22:
                    return "Hard";
                default:
                    return string.Empty;
            }
        }
    }
}