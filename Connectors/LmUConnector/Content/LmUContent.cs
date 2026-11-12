namespace SecondMonitor.LmUConnector.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NLog;

    using SecondMonitor.DataModel.Extensions;

    public class LmUCar
    {
        public LmUCar(string carName, string teamName, int number)
        {
            CarName = carName;
            TeamName = teamName;
            Number = number;
        }

        public string CarName { get; }
        public string TeamName { get; }
        public int Number { get; }
    }

    public class LmUContent
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        
        private static readonly string[] suffixes =
        [
            ":EC", ":AL", ":LM", ":PL", ":BE", ":BR", ":BL", ":JKR", ":US", ":SP", ":SE", ":EC2", ":EC3", ":QA", ":IT", ":JP", ":PR", ":FB", ":PS", ":PM", ":G1", ":G2", ":WE2",
            ":MZ", ":WEC", ":ELMS", ":ELMS25",
        ];
        private static readonly Dictionary<string, LmUCar> contentRaw = new Dictionary<string, LmUCar>()
        {
            { "Alpine Endurance Team 2024 #35", new LmUCar(Cars.AlpineA424, Teams.AlpineEndurance, 35) },
            { "Alpine Endurance Team 2024 #36", new LmUCar(Cars.AlpineA424, Teams.AlpineEndurance, 36) },
            { "BMW M Team WRT 2024 #15", new LmUCar(Cars.BmwHybridV8, Teams.BmwMTeam, 15) },
            { "BMW M Team WRT 2024 #20", new LmUCar(Cars.BmwHybridV8, Teams.BmwMTeam, 20) },
            { "Cadillac Racing 2024 #2", new LmUCar(Cars.CadillacVSeries, Teams.CadillacRacing, 2) },
            { "Cadillac Racing 2024 #3", new LmUCar(Cars.CadillacVSeries, Teams.CadillacRacing, 3) },
            { "Cadillac Racing #2", new LmUCar(Cars.CadillacVSeries, Teams.CadillacRacing, 2) },
            { "Cadillac Racing #3", new LmUCar(Cars.CadillacVSeries, Teams.CadillacRacing, 3) },
            { "Action Express Racing #311", new LmUCar(Cars.CadillacVSeries, Teams.ActionExpressRacing, 311) },
            { "Whelen Cadillac Racing 2024 #311", new LmUCar(Cars.CadillacVSeries, Teams.WhelenCadillacRacing, 311) },
            { "Ferrari AF Corse 2024 #50", new LmUCar(Cars.Ferrari499P, Teams.FerrariAfCorse, 50) },
            { "Ferrari AF Corse 2024 #51", new LmUCar(Cars.Ferrari499P, Teams.FerrariAfCorse, 51) },
            { "Ferrari AF Corse #50", new LmUCar(Cars.Ferrari499P, Teams.FerrariAfCorse, 50) },
            { "Ferrari AF Corse #51", new LmUCar(Cars.Ferrari499P, Teams.FerrariAfCorse, 51) },
            { "Richard Mille AF Corse 2025 #50", new LmUCar(Cars.Ferrari296GT3, Teams.RichardMilleAfCorse, 50) },
            { "AF Corse 2024 #83", new LmUCar(Cars.Ferrari499P, Teams.AfCorse, 83) },
            { "Isotta TIPO6 2024 #11", new LmUCar(Cars.Isotta, Teams.IsottaTipo6, 11) },
            { "Lamborghini Iron Lynx 2024 #19", new LmUCar(Cars.LamborghiniSc63, Teams.LamborghiniIronLynx, 19) },
            { "Lamborghini Iron Lynx 2024 #63", new LmUCar(Cars.LamborghiniSc63, Teams.LamborghiniIronLynx, 63) },
            { "Peugeot TotalEnergies 2024 #93", new LmUCar(Cars.Peugeot9X8, Teams.Peugeot, 93) },
            { "Peugeot TotalEnergies 2024 #94", new LmUCar(Cars.Peugeot9X8, Teams.Peugeot, 94) },
            { "Peugeot TotalEnergies #93", new LmUCar(Cars.Peugeot9X8, Teams.Peugeot, 93) },
            { "Peugeot TotalEnergies #94", new LmUCar(Cars.Peugeot9X8, Teams.Peugeot, 94) },
            { "Porsche Penske Motorsport 2024 #4", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 4) },
            { "Porsche Penske Motorsport 2024 #5", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 5) },
            { "Porsche Penske Motorsport 2024 #6", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 6) },
            { "Porsche Penske Motorsport #4", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 4) },
            { "Porsche Penske Motorsport #5", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 5) },
            { "Porsche Penske Motorsport #6", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 6) },
            { "Porsche Penske Motorsport #75", new LmUCar(Cars.Porsche963, Teams.PorschePenske, 75) },
            { "Toyota Gazoo Racing #7", new LmUCar(Cars.ToyotaGR010, Teams.ToyotaRacing, 7) },
            { "Toyota Gazoo Racing #8", new LmUCar(Cars.ToyotaGR010, Teams.ToyotaRacing, 8) },
            { "Toyota Gazoo Racing 2024 #7", new LmUCar(Cars.ToyotaGR010, Teams.ToyotaRacing, 7) },
            { "Toyota Gazoo Racing 2024 #8", new LmUCar(Cars.ToyotaGR010, Teams.ToyotaRacing, 8) },
            { "Hertz Team Jota 2024 #12", new LmUCar(Cars.Porsche963, Teams.HertzTeamJota, 12) },
            { "Hertz Team Jota 2024 #38", new LmUCar(Cars.Porsche963, Teams.HertzTeamJota, 38) },
            { "Hertz Team Jota #12", new LmUCar(Cars.Porsche963, Teams.HertzTeamJota, 12) },
            { "Hertz Team Jota #38", new LmUCar(Cars.Porsche963, Teams.HertzTeamJota, 38) },
            { "Cadillac Hertz Team Jota 2025 #12", new LmUCar(Cars.CadillacVSeries, Teams.CadillacHertzTeamJota, 12) },
            { "Cadillac Hertz Team Jota 2025 #38", new LmUCar(Cars.CadillacVSeries, Teams.CadillacHertzTeamJota, 38) },
            { "Proton Competition 2024 #99", new LmUCar(Cars.Porsche963, Teams.ProtonCompetition, 99) },
            { "Glickenhaus Racing #708", new LmUCar(Cars.GlickenhausSCG007, Teams.GlickenHouse, 708) },
            { "Glickenhaus Racing #709", new LmUCar(Cars.GlickenhausSCG007, Teams.GlickenHouse, 709) },
            { "Floyd Vanwall Racing Team #4", new LmUCar(Cars.VanwallVandervell, Teams.Vanwall, 4) },
            { "Vista AF Corse 2025 #21", new LmUCar(Cars.Ferrari296GT3, Teams.VistaAFCorse, 21) },
            { "Vista AF Corse 2024 #54", new LmUCar(Cars.Ferrari296GT3, Teams.VistaAFCorse, 54) },
            { "Vista AF Corse 2024 #55", new LmUCar(Cars.Ferrari296GT3, Teams.VistaAFCorse, 55) },
            { "United Autosports 2024 #59", new LmUCar(Cars.McLaren720sGT3, Teams.UnitedAutosport, 59) },
            { "United Autosports 2024 #95", new LmUCar(Cars.McLaren720sGT3, Teams.UnitedAutosport, 95) },
            { "United Autosports 2025 #23", new LmUCar(Cars.McLaren720sGT3, Teams.UnitedAutosport, 23) },
            { "TF Sport #81", new LmUCar(Cars.ChevroletCorvetteGT3, Teams.TfSport, 81) },
            { "TF Sport #82", new LmUCar(Cars.ChevroletCorvetteGT3, Teams.TfSport, 82) },
            { "TF Sport 2025 #81", new LmUCar(Cars.ChevroletCorvetteGT3, Teams.TfSport, 81) },
            { "TF Sport 2025 #82", new LmUCar(Cars.ChevroletCorvetteGT3, Teams.TfSport, 82) },
            { "TF Sport 2025 #33", new LmUCar(Cars.ChevroletCorvetteGTE, Teams.TfSport, 33) },
            { "Team WRT 2024 #31", new LmUCar(Cars.BMWM4Gt3, Teams.TeamWRT, 31) },
            { "Team WRT 2024 #46", new LmUCar(Cars.BMWM4Gt3, Teams.TeamWRT, 46) },
            { "JMW Motorsport 2024 2024 #66", new LmUCar(Cars.Ferrari296GT3, Teams.JMWMotosport, 66) },
            { "Inception Racing 2024 #70", new LmUCar(Cars.McLaren720sGT3, Teams.InceptionRacing, 70) },
            { "GR Racing 2024 #86", new LmUCar(Cars.Ferrari296GT3, Teams.GrRacing, 86) },
            { "Spirit Of Race 2024 #155", new LmUCar(Cars.Ferrari296GT3, Teams.SpiritOfRace, 155) },
            { "Heart of Racing Team 2024 #27", new LmUCar(Cars.AstonMartinGT3, Teams.HeartRacingTeam, 27) },
            { "D'Station Racing 2024 #777", new LmUCar(Cars.AstonMartinGT3, Teams.DstationRacing, 777) },
            { "Proton Racing 2024 #44", new LmUCar(Cars.FordMustangGT3, Teams.Proton, 44) },
            { "Proton Racing 2024 #77", new LmUCar(Cars.FordMustangGT3, Teams.Proton, 77) },
            { "Proton Competition #77", new LmUCar(Cars.Oreca07, Teams.Proton, 77) },
            { "Proton Competition 2025 #77", new LmUCar(Cars.FordMustangGT3, Teams.Proton, 77) },
            { "Proton Competition 2025 #88", new LmUCar(Cars.FordMustangGT3, Teams.Proton, 88) },
            { "Proton Racing 2024 #88", new LmUCar(Cars.FordMustangGT3, Teams.Proton, 88) },
            { "Manthey Ema 2024 #91", new LmUCar(Cars.Porsche911GT3, Teams.MantheyEMA, 91) },
            { "Manthey PureRxcing 2024 #92", new LmUCar(Cars.Porsche911GT3, Teams.MantheyPureRxcing, 92) },
            { "Proton Competition 2024 #9", new LmUCar(Cars.Oreca07, Teams.ProtonCompetition, 9) },
            { "Vector Sport 2024 #10", new LmUCar(Cars.Oreca07, Teams.VectorSport, 10) },
            { "AO by TF 2024 #14", new LmUCar(Cars.Oreca07, Teams.AObyTF, 14) },
            { "AO by TF #99", new LmUCar(Cars.Oreca07, Teams.AObyTF, 99) },
            { "United Autosports 2024 #22", new LmUCar(Cars.Oreca07, Teams.UnitedAutosport, 22) },
            { "United Autosports USA 2024 #23", new LmUCar(Cars.Oreca07, Teams.UnitedAutosportUSA, 23) },
            { "Nielsen Racing 2024 #14", new LmUCar(Cars.Oreca07, Teams.NielsenRacing, 14) },
            { "Nielsen Racing #24", new LmUCar(Cars.Oreca07, Teams.NielsenRacing, 24) },
            { "Nielsen Racing 2024 #24", new LmUCar(Cars.Oreca07, Teams.NielsenRacing, 24) },
            { "Algarve Pro Racing 2024 #25", new LmUCar(Cars.Oreca07, Teams.AlgarveProRacing, 25) },
            { "IDEC Sport 2024 #28", new LmUCar(Cars.Oreca07, Teams.IdecSport, 28) },
            { "IDEC Sport #18", new LmUCar(Cars.Oreca07, Teams.IdecSport, 18) },
            { "Duqueine Team #30", new LmUCar(Cars.Oreca07, Teams.DuqueineTeam, 30) },
            { "Duqueine Team 2024 #30", new LmUCar(Cars.Oreca07, Teams.DuqueineTeam, 30) },
            { "DKR Engineering 2024 #33", new LmUCar(Cars.Oreca07, Teams.DkrEngineering, 33) },
            { "Inter Europol Competition 2024 #34", new LmUCar(Cars.Oreca07, Teams.InterEuropol, 34) },
            { "Cool Racing 2024 #37", new LmUCar(Cars.Oreca07, Teams.CoolRacing, 37) },
            { "Cool Racing 2024 #47", new LmUCar(Cars.Oreca07, Teams.CoolRacing, 47) },
            { "Crowdstrike Racing by APR 2024 #45", new LmUCar(Cars.Oreca07, Teams.CrowdstrikeRacingAPR, 45) },
            { "Panis Racing 2024 #65", new LmUCar(Cars.Oreca07, Teams.PanisRacing, 65) },
            { "VDS Panis Racing #48", new LmUCar(Cars.Oreca07, Teams.VdsPanisRacing, 48) },
            { "AF Corse 2024 #183", new LmUCar(Cars.Oreca07, Teams.AfCorse, 183) },
            { "Proton Competition #9", new LmUCar(Cars.Oreca07, Teams.ProtonCompetition, 9) },
            { "Vector Sport #10", new LmUCar(Cars.Oreca07, Teams.VectorSport, 10) },
            { "AO by TF #14", new LmUCar(Cars.Oreca07, Teams.AObyTF, 14) },
            { "United Autosports #21", new LmUCar(Cars.Oreca07, Teams.UnitedAutosport, 21) },
            { "United Autosports #22", new LmUCar(Cars.Oreca07, Teams.UnitedAutosport, 22) },
            { "United Autosports #23", new LmUCar(Cars.Oreca07, Teams.UnitedAutosport, 23) },
            { "United Autosports USA #23", new LmUCar(Cars.Oreca07, Teams.UnitedAutosportUSA, 23) },
            { "Nielsen Racing #14", new LmUCar(Cars.Oreca07, Teams.NielsenRacing, 14) },
            { "Nielsen Racing #27", new LmUCar(Cars.Oreca07, Teams.NielsenRacing, 27) },
            { "Algarve Pro Racing #25", new LmUCar(Cars.Oreca07, Teams.AlgarveProRacing, 25) },
            { "Algarve Pro Racing #20", new LmUCar(Cars.Oreca07, Teams.AlgarveProRacing, 20) },
            { "IDEC Sport #28", new LmUCar(Cars.Oreca07, Teams.IdecSport, 28) },
            { "IDEC Sport #48", new LmUCar(Cars.Oreca07, Teams.IdecSport, 48) },
            { "DKR Engineering #4", new LmUCar(Cars.GinnettaG61, Teams.DkrEngineering, 4) },
            { "DKR Engineering #3", new LmUCar(Cars.Oreca07, Teams.DkrEngineering, 3) },
            { "DKR Engineering #33", new LmUCar(Cars.Oreca07, Teams.DkrEngineering, 33) },
            { "DKR Engineering #43", new LmUCar(Cars.Oreca07, Teams.DkrEngineering, 43) },
            { "Inter Europol Competition #34", new LmUCar(Cars.Oreca07, Teams.InterEuropol, 34) },
            { "Inter Europol Competition #31", new LmUCar(Cars.Oreca07, Teams.InterEuropol, 31) },
            { "Inter Europol Competition #32", new LmUCar(Cars.Oreca07, Teams.InterEuropol, 32) },
            { "Inter Europol Competition #43", new LmUCar(Cars.Oreca07, Teams.InterEuropol, 43) },
            { "Cool Racing #37", new LmUCar(Cars.Oreca07, Teams.CoolRacing, 37) },
            { "Cool Racing #47", new LmUCar(Cars.Oreca07, Teams.CoolRacing, 47) },
            { "Crowdstrike Racing by APR #45", new LmUCar(Cars.Oreca07, Teams.CrowdstrikeRacingAPR, 45) },
            { "Panis Racing #65", new LmUCar(Cars.Oreca07, Teams.PanisRacing, 65) },
            { "AF Corse #80", new LmUCar(Cars.Oreca07, Teams.AfCorse, 80) },
            { "AF Corse #183", new LmUCar(Cars.Oreca07, Teams.AfCorse, 183) },
            { "Prema Racing #9", new LmUCar(Cars.Oreca07, Teams.PremaRacing, 9) },
            { "Prema Racing #63", new LmUCar(Cars.Oreca07, Teams.PremaRacing, 63) },
            { "JOTA #28", new LmUCar(Cars.Oreca07, Teams.Jota, 28) },
            { "Tower Motorsports #13", new LmUCar(Cars.Oreca07, Teams.TowerMotosport, 13) },
            { "Team WRT #31", new LmUCar(Cars.Oreca07, Teams.TeamWRT, 31) },
            { "Team WRT #41", new LmUCar(Cars.Oreca07, Teams.TeamWRT, 41) },
            { "Alpine Elf Team #35", new LmUCar(Cars.Oreca07, Teams.AlpineElf, 35) },
            { "Alpine Elf Team #36", new LmUCar(Cars.Oreca07, Teams.AlpineElf, 36) },
            { "Graff Racing #39", new LmUCar(Cars.Oreca07, Teams.GraffRacing, 39) },
            { "Algarve Pro Racing #45", new LmUCar(Cars.Oreca07, Teams.AlgarveProRacing, 45) },
            { "Racing Team Turkey #923", new LmUCar(Cars.Oreca07, Teams.RacingTeamTurkey, 923) },
            { "TDS Racing #29", new LmUCar(Cars.Oreca07, Teams.TDSRacing, 29) },
            { "ORT by TF #25", new LmUCar(Cars.AstonMartinGTE, Teams.ORT, 25) },
            { "GMB Motorsport #55", new LmUCar(Cars.AstonMartinGTE, Teams.GMBMotorsport, 55) },
            { "TF Sport #72", new LmUCar(Cars.AstonMartinGTE, Teams.TfSport, 72) },
            { "The Heart of Racing #98", new LmUCar(Cars.AstonMartinGTE, Teams.HeartRacingTeam, 98) },
            { "Northwest AMR #98", new LmUCar(Cars.AstonMartinGTE, Teams.NorthWest, 98) },
            { "D'station Racing #777", new LmUCar(Cars.AstonMartinGTE, Teams.DstationRacing, 777) },
            { "Corvette Racing #33", new LmUCar(Cars.ChevroletCorvetteGTE, Teams.CorvetteRacing, 33) },
            { "AF Corse #21", new LmUCar(Cars.Ferrari488GTE, Teams.AfCorse, 21) },
            { "AF Corse #54", new LmUCar(Cars.Ferrari488GTE, Teams.AfCorse, 54) },
            { "AF Corse #83", new LmUCar(Cars.Ferrari488GTE, Teams.AfCorse, 83) },
            { "AF Corse #83:ELMS25", new LmUCar(Cars.Oreca07, Teams.AfCorse, 83) },
            { "AF Corse 2025 #51", new LmUCar(Cars.Ferrari296GT3, Teams.AfCorse, 51) },
            { "Kessel Racing #57", new LmUCar(Cars.Ferrari488GTE, Teams.KesselRacing, 57) },
            { "Kessel Racing #74", new LmUCar(Cars.Ferrari488GTE, Teams.KesselRacing, 74) },
            { "Kessel Racing 2025 #57", new LmUCar(Cars.Ferrari296GT3, Teams.KesselRacing, 57) },
            { "Kessel Racing 2025 #74", new LmUCar(Cars.Ferrari296GT3, Teams.KesselRacing, 74) },
            { "JMW Motorsport #66", new LmUCar(Cars.Ferrari488GTE, Teams.JMWMotosport, 66) },
            { "JMW Motorsport 2025 #66", new LmUCar(Cars.Ferrari296GT3, Teams.JMWMotosport, 66) },
            { "Walkenhorst Motorsport #100", new LmUCar(Cars.Ferrari488GTE, Teams.WalkenhorstMotorsport, 100) },
            { "Proton Competition #16", new LmUCar(Cars.Porsche911GTE, Teams.ProtonCompetition, 16) },
            { "Proton Competition #60", new LmUCar(Cars.Porsche911GTE, Teams.ProtonCompetition, 60) }, 
            { "Proton Competition 2025 #60", new LmUCar(Cars.Porsche911GTE, Teams.ProtonCompetition, 60) },
            { "Proton Competition #88", new LmUCar(Cars.Porsche911GTE, Teams.ProtonCompetition, 88) },
            { "Proton Competition #911", new LmUCar(Cars.Porsche911GTE, Teams.ProtonCompetition, 911) },
            { "Project 1 - AO #56", new LmUCar(Cars.Porsche911GTE, Teams.Project1, 56) },
            { "Iron Lynx #60", new LmUCar(Cars.Porsche911GTE, Teams.IronLynx, 60) },
            { "Iron Lynx - Proton #9", new LmUCar(Cars.Oreca07, Teams.IronLynxProton, 9) },
            { "Iron Lynx 2025 #61", new LmUCar(Cars.MercedesAMGGT3, Teams.IronLynx, 61) },
            { "Iron Lynx 2025 #63", new LmUCar(Cars.MercedesAMGGT3, Teams.IronLynx, 63) },
            { "Dempsey-Proton Racing #77", new LmUCar(Cars.Porsche911GTE, Teams.Dempsey, 77) },
            { "Iron Dames #85", new LmUCar(Cars.Porsche911GTE, Teams.IronDames, 85) },
            { "GR Racing #86", new LmUCar(Cars.Porsche911GTE, Teams.GrRacing, 86) },
            { "Akkodis ASP Team 2024 #78", new LmUCar(Cars.LexusRcGT3, Teams.Akkodis, 78) },
            { "Akkodis ASP Team 2024 #87", new LmUCar(Cars.LexusRcGT3, Teams.Akkodis, 87) },
            { "Iron Dames 2024 #85", new LmUCar(Cars.LamborghiniHuracanGT3, Teams.IronDames, 85) },
            { "Iron Lynx 2024 #60", new LmUCar(Cars.LamborghiniHuracanGT3, Teams.IronLynx, 60) },
            { "Aston Martin THOR Team 2025 #007", new LmUCar(Cars.AstonMartingValkyrie, Teams.AstonMartinThor, 7) },
            { "Aston Martin THOR Team 2025 #009", new LmUCar(Cars.AstonMartingValkyrie, Teams.AstonMartinThor, 9) },
            { "Manthey 1st Phorm 2025 #92", new LmUCar(Cars.Porsche911GT3, Teams.MantheyFirstPhorm, 92) },
            { "Racing Spirit of Léman 2025 #10", new LmUCar(Cars.AstonMartinGT3, Teams.RacingSpritOfLeman, 10) },
            { "Racing Spirit of Léman 2025 #59", new LmUCar(Cars.AstonMartinGT3, Teams.RacingSpritOfLeman, 59) },
            { "The Bend Team WRT 2025 #31", new LmUCar(Cars.BMWM4Gt3, Teams.TheBendTeam, 31) },
            { "Spirit of Race 2025 #55", new LmUCar(Cars.Ferrari296GT3, Teams.RacingSpritOfLeman, 55) },
            { "Team Virage #8", new LmUCar(Cars.LigierP325, Teams.TeamVirage, 8) },
            { "EuroInternational #11", new LmUCar(Cars.LigierP325, Teams.EuroInternation, 11) },
            { "RLR MSport #15", new LmUCar(Cars.LigierP325, Teams.RlrMsport, 15) },
            { "CLX Motorsport #17", new LmUCar(Cars.LigierP325, Teams.ClxMotosport, 17) },
            { "CLX Motorsport #47", new LmUCar(Cars.Oreca07, Teams.ClxMotosport, 47) },
            { "CLX - Pure Rxcing #37", new LmUCar(Cars.Oreca07, Teams.ClxPureRacing, 37) },
            { "Racing Spirit of Léman #31", new LmUCar(Cars.LigierP325, Teams.RacingSpritOfLeman, 31) },
            { "Ultimate #35", new LmUCar(Cars.LigierP325, Teams.Ultimate, 35) },
            { "M Racing #68", new LmUCar(Cars.LigierP325, Teams.MRacing, 68) },
            { "Inter Europol Competition #88", new LmUCar(Cars.LigierP325, Teams.InterEuropol, 88) },
            { "Iron Dames 2025 #85:ELMS", new LmUCar(Cars.Porsche911GTE, Teams.IronDames, 85) },
        };

        private readonly Dictionary<string, LmUCar> _content;

        public LmUContent()
        {
            foreach (string key in contentRaw.Keys.ToList())
            {
                if (key.Contains(" 2024"))
                {
                    contentRaw[key.Replace(" 2024", " 2025")] = contentRaw[key];
                }
            }

            _content = contentRaw.ToDictionary();

            foreach (string suffix in suffixes)
            {
                contentRaw.ForEach(x => _content.TryAdd(x.Key + suffix, x.Value));
            }
        }

        public LmUCar GetCarSafe(string lmuCarName)
        {
            return _content.TryGetValue(lmuCarName, out LmUCar lmuCar) ? lmuCar : TryParseLmuCarName(lmuCarName);
        }

        private LmUCar TryParseLmuCarName(string lmuCarName)
        {
            try
            {
                string parsedLmuCarName = lmuCarName;
                suffixes.ForEach(x => parsedLmuCarName = parsedLmuCarName.Replace(x, string.Empty));
                string[] entries = parsedLmuCarName.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                LmUCar lmUCar;
                if (entries.Length < 3 || !int.TryParse(entries[^1].Replace("#", string.Empty), out int number))
                {
                    lmUCar = new LmUCar(string.Empty, string.Empty, 0);
                    _content.Add(lmuCarName, lmUCar);
                    return lmUCar;
                }

                string team = string.Join(" ", entries.Take(entries.Length - 2));
                lmUCar = new LmUCar(string.Empty, team, number);
                _content.Add(lmuCarName, lmUCar);
                return lmUCar;
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to parse LMU car name '{lmuCarName}': {ex.Message}");
                LmUCar lmUCar = new LmUCar(string.Empty, string.Empty, 0);
                _content.Add(lmuCarName, lmUCar);
                return lmUCar;
            }
        }
    }
}
