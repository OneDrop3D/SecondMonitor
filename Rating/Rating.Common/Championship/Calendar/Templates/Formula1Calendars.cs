namespace SecondMonitor.Rating.Common.Championship.Calendar.Templates
{
    using Tracks;

    public static class Formula1Calendars
    {
        public static CalendarTemplateGroup Formula1AllGroup => new CalendarTemplateGroup("Formula 1", new[] { Formula190s, Formula120092013, Formula1V6HybridEra, Formula160s, Formula170s, Formula12000s });

        public static CalendarTemplateGroup Formula160s => new CalendarTemplateGroup("1960 - 1969", new[] { Formula11962, Formula11967 });

        public static CalendarTemplateGroup Formula170s => new CalendarTemplateGroup("1970 - 1979", new[] { Formula11970, Formula11972, Formula11976 });

        public static CalendarTemplateGroup Formula190s => new CalendarTemplateGroup("1990 - 1999", new[] { Formula11993, Formula11996, Formula11998 });

        public static CalendarTemplateGroup Formula12000s => new CalendarTemplateGroup("2000 - 2008", new[] { Formula12003 });

        public static CalendarTemplateGroup Formula120092013 => new CalendarTemplateGroup("2009 - 2013", new[] { Formula12013 });

        public static CalendarTemplateGroup Formula1V6HybridEra => new CalendarTemplateGroup("2014 - Present (V6 Hybrid Era)", new[] { Formula12019, Formula12020, Formula12022 });

        public static CalendarTemplate Formula11962 => new CalendarTemplate("1962 - Formula 1", 1962, new[]
        {
            new EventTemplate(TracksTemplates.Zandvoort4878, "Dutch Grand Prix"),
            new EventTemplate(TracksTemplates.Monaco5571, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.SpaHistoric, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.RouenLesEssarts, "French Grand Prix"),
            new EventTemplate(TracksTemplates.AintreeGrandPrix, "British Grand Prix"),
            new EventTemplate(TracksTemplates.NordschleifeHistoric, "German Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaRoad5971, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.WatkinsGlenGp5670, "United States Grand Prix"),
            new EventTemplate(TracksTemplates.PrinceGeorge, "South African Grand Prix"),
        });

        public static CalendarTemplate Formula11967 => new CalendarTemplate("1967 - Formula 1", 1967, new[]
        {
            new EventTemplate(TracksTemplates.KyalamiGP6187, "South African Grand Prix"),
            new EventTemplate(TracksTemplates.Monaco5571, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.Zandvoort4878, "Dutch Grand Prix"),
            new EventTemplate(TracksTemplates.SpaHistoric, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.LeMansBugatti, "French Grand Prix"),
            new EventTemplate(TracksTemplates.SilverstoneGp5274, "British Grand Prix"),
            new EventTemplate(TracksTemplates.NordschleifeHistoric, "German Grand Prix"),
            new EventTemplate(TracksTemplates.CanadianTireMotosportPark, "Canadian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaRoad5971, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.WatkinsGlenGp5670, "United States Grand Prix"),
            new EventTemplate(TracksTemplates.MexicoGp5985, "Mexican Grand Prix"),
        });

        public static CalendarTemplate Formula11970 => new CalendarTemplate("1970 - Formula 1", 1970, new[]
        {
            new EventTemplate(TracksTemplates.KyalamiGP6187, "South African Grand Prix"),
            new EventTemplate(TracksTemplates.JaramaGp6679, "Spanish Grand Prix"),
            new EventTemplate(TracksTemplates.Monaco5571, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.SpaHistoric, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.Zandvoort4878, "Dutch Grand Prix"),
            new EventTemplate(TracksTemplates.CharadeGrandPrix5888, "French Grand Prix"),
            new EventTemplate(TracksTemplates.BrandsHatchGp6075, "British Grand Prix"),
            new EventTemplate(TracksTemplates.HockenheimringGp7080, "German Grand Prix"),
            new EventTemplate(TracksTemplates.RedBullOsterreichring, "Austrian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaRoad5971, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.CircuitMontTremblantFull, "Canadian Grand Prix"),
            new EventTemplate(TracksTemplates.WatkinsGlenGp5670, "United States Grand Prix"),
            new EventTemplate(TracksTemplates.MexicoGp5985, "Mexican Grand Prix"),
        });

        public static CalendarTemplate Formula11972 => new CalendarTemplate("1972 - Formula 1", 1972, new[]
        {
            new EventTemplate(TracksTemplates.BuenosAiresNo9, "Argentine Grand Prix"),
            new EventTemplate(TracksTemplates.KyalamiGP6187, "South African Grand Prix"),
            new EventTemplate(TracksTemplates.JaramaGp6679, "Spanish Grand Prix"),
            new EventTemplate(TracksTemplates.Monaco72, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.NivellesBaulers, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.CharadeGrandPrix5888, "French Grand Prix"),
            new EventTemplate(TracksTemplates.BrandsHatchGp6075, "British Grand Prix"),
            new EventTemplate(TracksTemplates.NordschleifeHistoric, "German Grand Prix"),
            new EventTemplate(TracksTemplates.RedBullOsterreichring, "Austrian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaRoad7275, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.CanadianTireMotosportPark, "Canadian Grand Prix"),
            new EventTemplate(TracksTemplates.WatkinsGlenGp7174, "United States Grand Prix"),
        });

        public static CalendarTemplate Formula11976 => new CalendarTemplate("1976 - Formula 1", 1976, new[]
        {
            new EventTemplate(TracksTemplates.InterlagosGp4089, "Brazilian Grand Prix"),
            new EventTemplate(TracksTemplates.KyalamiGP6187, "South African Grand Prix"),
            new EventTemplate(TracksTemplates.LongBeach, "United States Grand Prix West"),
            new EventTemplate(TracksTemplates.JaramaGp6679, "Spanish Grand Prix"),
            new EventTemplate(TracksTemplates.ZolderGp7581, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.Monaco7685, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.Anderstorp7292, "Swedish Grand Prix"),
            new EventTemplate(TracksTemplates.PaulRicardGp7001, "French Grand Prix"),
            new EventTemplate(TracksTemplates.BrandsHatchGp7687, "British Grand Prix"),
            new EventTemplate(TracksTemplates.NordschleifeHistoric, "German Grand Prix"),
            new EventTemplate(TracksTemplates.RedBullOsterreichring, "Austrian Grand Prix"),
            new EventTemplate(TracksTemplates.Zandvoort4878, "Dutch Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaGp7694, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.CanadianTireMotosportPark, "Canadian Grand Prix"),
            new EventTemplate(TracksTemplates.WatkinsGlenGp7583, "United States Grand Prix"),
            new EventTemplate(TracksTemplates.FujiGp8485, "Japanese Grand Prix"),
        });

        public static CalendarTemplate Formula11993 => new CalendarTemplate("1993 - Formula 1", 1993, new[]
        {
            new EventTemplate(TracksTemplates.KyalamiGP9308, "South African Grand Prix"),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "Brazilian Grand Prix"),
            new EventTemplate(TracksTemplates.DoningtonParkGP8609, "European Grand Prix"),
            new EventTemplate(TracksTemplates.ImolaGp8594, "San Marino Grand Prix"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGp9194, "Spanish Grand Prix"),
            new EventTemplate(TracksTemplates.Monaco8696, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.MontrealGp9193, "Canadian Grand Prix"),
            new EventTemplate(TracksTemplates.MagnyCourseGp92to02, "French Grand Prix"),
            new EventTemplate(TracksTemplates.SilverstoneGp9193, "British Grand Prix"),
            new EventTemplate(TracksTemplates.HockenheimringGp92to01, "German Grand Prix"),
            new EventTemplate(TracksTemplates.Hungaroring8902, "Hungarian Grand Prix"),
            new EventTemplate(TracksTemplates.Spa8393, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaGp7694, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.EstorilGp72to93, "Portuguese Grand Prix"),
            new EventTemplate(TracksTemplates.SuzukaGP9199, "Japanese Grand Prix"),
            new EventTemplate(TracksTemplates.AdelaideGp8595, "Australian Grand Prix"),
        });

        public static CalendarTemplate Formula11996 => new CalendarTemplate("1996 - Formula 1", 1996, new[]
        {
            new EventTemplate(TracksTemplates.AlbertPark1120, "Transurban Australian Grand Prix"),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "Grande Premio do Brasil"),
            new EventTemplate(TracksTemplates.BuenosAiresNo6, "Gran Premio de Argentina"),
            new EventTemplate(TracksTemplates.NurburgringGp84to02, "Grand Prix of Europe"),
            new EventTemplate(TracksTemplates.ImolaGp9506, "Gran Premio di San Marino"),
            new EventTemplate(TracksTemplates.Monaco8696, "Grand Prix de Monaco"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGp9503, "Gran Premio Marlboro de Espana"),
            new EventTemplate(TracksTemplates.MontrealGp9601, "Grand Prix Molson du Canada"),
            new EventTemplate(TracksTemplates.MagnyCourseGp92to02, "Grand Prix de France"),
            new EventTemplate(TracksTemplates.SilverstoneGp96, "RAC British Grand Prix"),
            new EventTemplate(TracksTemplates.HockenheimringGp92to01, "Grosser Mobil 1 Preis von Deutschland"),
            new EventTemplate(TracksTemplates.Hungaroring8902, "Marlboro Magyar Nagydij"),
            new EventTemplate(TracksTemplates.Spa9501, "Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaGp9599, "Pioneer Gran Premio d'Italia"),
            new EventTemplate(TracksTemplates.EstorilGp94to99, "Grande Premio de Portugal"),
            new EventTemplate(TracksTemplates.SuzukaGP9199, "Fuji Television Japanese Grand Prix"),
        });

        public static CalendarTemplate Formula11998 => new CalendarTemplate("1998 - Formula 1", 1998, new[]
        {
            new EventTemplate(TracksTemplates.AlbertPark1120, "Qantas Australian Grand Prix"),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "Grande Premio Marlboro do Brasil"),
            new EventTemplate(TracksTemplates.BuenosAiresNo6, "Gran Premio Marlboro Argentina"),
            new EventTemplate(TracksTemplates.ImolaGp9506, "Gran Premio di San Marino"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGp9503, "Gran Premio Marlboro de Espana"),
            new EventTemplate(TracksTemplates.Monaco9702, "Grand Prix de Monaco"),
            new EventTemplate(TracksTemplates.MontrealGp9601, "Grand Prix Player's du Canada"),
            new EventTemplate(TracksTemplates.MagnyCourseGp92to02, "Mobil 1 Grand Prix de France"),
            new EventTemplate(TracksTemplates.SilverstoneGp9709, "RAC British Grand Prix"),
            new EventTemplate(TracksTemplates.RedBullA1Ring, "Grosser Preis von Osterreich"),
            new EventTemplate(TracksTemplates.HockenheimringGp92to01, "Grosser Mobil 1 Preis von Deutschland"),
            new EventTemplate(TracksTemplates.Hungaroring8902, "Marlboro Magyar Nagydij"),
            new EventTemplate(TracksTemplates.Spa9501, "Foster's Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaGp9599, "Gran Premio Campari d'Italia"),
            new EventTemplate(TracksTemplates.NurburgringGp84to02, "Grosser Warsteiner Preis von Luxemburg"),
            new EventTemplate(TracksTemplates.SuzukaGP9199, "Fuji Television Japanese Grand Prix"),
        });

        public static CalendarTemplate Formula12003 => new CalendarTemplate("2003 - Formula 1", 2003, new[]
        {
            new EventTemplate(TracksTemplates.AlbertPark1120, "Australian Grand Prix"),
            new EventTemplate(TracksTemplates.SepangGPPresent, "Malaysian Grand Prix"),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "Brazilian Grand Prix"),
            new EventTemplate(TracksTemplates.ImolaGp9506, "San Marino Grand Prix"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGp9503, "Spanish Grand Prix"),
            new EventTemplate(TracksTemplates.RedBullA1Ring, "Austrian Grand Prix"),
            new EventTemplate(TracksTemplates.MonacoPresent, "Monaco Grand Prix"),
            new EventTemplate(TracksTemplates.MontrealGpPresent, "Canadian Grand Prix"),
            new EventTemplate(TracksTemplates.NurburgringGpPresent, "European Grand Prix"),
            new EventTemplate(TracksTemplates.MagnyCourseGpPresent, "French Grand Prix"),
            new EventTemplate(TracksTemplates.SilverstoneGp9709, "British Grand Prix"),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent, "German Grand Prix"),
            new EventTemplate(TracksTemplates.HungaroringPresent, "Hungarian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaGpPresent, "Italian Grand Prix"),
            new EventTemplate(TracksTemplates.IndianapolisMotorSpeedwayRoad0007, "United States Grand Prix"),
            new EventTemplate(TracksTemplates.SuzukaGPPresent, "Japanese Grand Prix"),
        });

        public static CalendarTemplate Formula12013 => new CalendarTemplate("2013 - Formula 1", 2013, new[]
        {
            new EventTemplate(TracksTemplates.AlbertPark1120),
            new EventTemplate(TracksTemplates.SepangGPPresent),
            new EventTemplate(TracksTemplates.ShanghaiGp),
            new EventTemplate(TracksTemplates.BahrainGP),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent),
            new EventTemplate(TracksTemplates.MonacoPresent),
            new EventTemplate(TracksTemplates.MontrealGpPresent),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent),
            new EventTemplate(TracksTemplates.HungaroringPresent),
            new EventTemplate(TracksTemplates.SpaPresent),
            new EventTemplate(TracksTemplates.MonzaGpPresent),
            new EventTemplate(TracksTemplates.Singapore),
            new EventTemplate(TracksTemplates.KoreaGp),
            new EventTemplate(TracksTemplates.SuzukaGPPresent),
            new EventTemplate(TracksTemplates.BudhGp),
            new EventTemplate(TracksTemplates.YasMarinaGrandPrix),
            new EventTemplate(TracksTemplates.CotaGP),
            new EventTemplate(TracksTemplates.InterlagosGpPresent),
        });

        public static CalendarTemplate Formula12019 => new CalendarTemplate("2019 - Formula 1", 2019, new[]
        {
            new EventTemplate(TracksTemplates.AlbertPark1120, "Formula 1 Rolex Australian Grand Prix 2019"),
            new EventTemplate(TracksTemplates.BahrainGP, "Formula 1 Gulf Air Bahrain Grand Prix 2019"),
            new EventTemplate(TracksTemplates.ShanghaiGp, "Formula 1 Heineken Chinese Grand Prix 2019"),
            new EventTemplate(TracksTemplates.BakuGP, "Formula 1 Socar Azerbaijan Grand Prix 2019"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent, "Formula 1 Emirates Gran Premio De España 2019"),
            new EventTemplate(TracksTemplates.MonacoPresent, "Formula 1 Grand Prix De Monaco 2019"),
            new EventTemplate(TracksTemplates.MontrealGpPresent, "Formula 1 Pirelli Grand Prix Du Canada 2019"),
            new EventTemplate(TracksTemplates.PaulRicard1CV2, "Formula 1 Pirelli Grand Prix De France 2019"),
            new EventTemplate(TracksTemplates.RedBullRing, "Formula 1 Myworld Grosser Preis Von Österreich 2019"),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent, "Formula 1 Rolex British Grand Prix 2019"),
            new EventTemplate(TracksTemplates.HockenheimringGpPresent, "Formula 1 Mercedes-Benz Grosser Preis Von Deutschland 2019"),
            new EventTemplate(TracksTemplates.HungaroringPresent, "Formula 1 Rolex Magyar Nagydíj 2019"),
            new EventTemplate(TracksTemplates.SpaPresent, "Formula 1 Johnnie Walker Belgian Grand Prix 2019"),
            new EventTemplate(TracksTemplates.MonzaGpPresent, "Formula 1 Gran Premio Heineken D'italia 2019"),
            new EventTemplate(TracksTemplates.Singapore, "Formula 1 Singapore Airlines Singapore Grand Prix 2019"),
            new EventTemplate(TracksTemplates.SochipGp, "Formula 1 Vtb Russian Grand Prix 2019"),
            new EventTemplate(TracksTemplates.SuzukaGPPresent, "Formula 1 Japanese Grand Prix 2019"),
            new EventTemplate(TracksTemplates.MexicoGpPresent, "Formula 1 Gran Premio De México 2019"),
            new EventTemplate(TracksTemplates.CotaGP, "Formula 1 Emirates United States Grand Prix 2019"),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "Formula 1 Heineken Grande Prêmio Do Brasil 2019"),
            new EventTemplate(TracksTemplates.YasMarinaGrandPrix, "Formula 1 Etihad Airways Abu Dhabi Grand Prix 2019")
        });

        public static CalendarTemplate Formula12020 => new CalendarTemplate("2020 - Formula 1 (Actual)", 2020, new[]
        {
            new EventTemplate(TracksTemplates.RedBullRing, "Formula 1 Rolex Grosser Preis von Osterreich"),
            new EventTemplate(TracksTemplates.RedBullRing, "Formula 1 Pirelli Grosser Preis der Steiermark"),
            new EventTemplate(TracksTemplates.HungaroringPresent, "Formula 1 Aramco Magyar Nagydij"),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent, "Formula 1 Pirelli British Grand Prix"),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent, "Emirates Formula 1 70th Anniversary Grand Prix"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent, "Formula 1 Aramco Gran Premio de Espana"),
            new EventTemplate(TracksTemplates.SpaPresent, "Formula 1 Rolex Belgian Grand Prix"),
            new EventTemplate(TracksTemplates.MonzaGpPresent, "Formula 1 Gran Premio Heineken d'Italia"),
            new EventTemplate(TracksTemplates.MugelloGpPresent, "Formula 1 Pirelli Gran Premio della Toscana Ferrari 1000"),
            new EventTemplate(TracksTemplates.SochipGp, "Formula 1 VTB Russian Grand Prix"),
            new EventTemplate(TracksTemplates.NurburgringGpPresent, "Formula 1 Aramco Großer Preis der Eifel"),
            new EventTemplate(TracksTemplates.AlgarveCircuit1Present, "Formula 1 Heineken Grande Prémio de Portugal"),
            new EventTemplate(TracksTemplates.ImolaGpPresent, "Formula 1 Emirates Gran Premio dell'Emilia Romagna"),
            new EventTemplate(TracksTemplates.IstanbulParkGp, "Formula 1 DHL Turkish Grand Prix"),
            new EventTemplate(TracksTemplates.BahrainGP, "Formula 1 Gulf Air Bahrain Grand Prix"),
            new EventTemplate(TracksTemplates.BahrainOuterCircuit, "Formula 1 Rolex Sakhir Grand Prix"),
            new EventTemplate(TracksTemplates.YasMarinaGrandPrix, "Formula 1 Etihad Airways Abu Dhabi Grand Prix"),
        });

        public static CalendarTemplate Formula12022 => new CalendarTemplate("2022 - Formula 1", 2022, new[]
        {
            new EventTemplate(TracksTemplates.BahrainGP, "FORMULA 1 GULF AIR BAHRAIN GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.JeddahCornicheCircuit, "FORMULA 1 STC SAUDI ARABIAN GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.AlbertParkPresent, "FORMULA 1 HEINEKEN AUSTRALIAN GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.ImolaGpPresent, "FORMULA 1 ROLEX GRAN PREMIO DEL MADE IN ITALY E DELL'EMILIA-ROMAGNA 2022"),
            new EventTemplate(TracksTemplates.MiamiF1Circuit, "FORMULA 1 CRYPTO.COM MIAMI GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.CircuitDeCatalunyaGpPresent, "FORMULA 1 PIRELLI GRAN PREMIO DE ESPAÑA 2022"),
            new EventTemplate(TracksTemplates.MonacoPresent, "FORMULA 1 GRAND PRIX DE MONACO 2022"),
            new EventTemplate(TracksTemplates.BakuGP, "FORMULA 1 AZERBAIJAN GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.MontrealGpPresent, "FORMULA 1 GRAND PRIX DU CANADA 2022"),
            new EventTemplate(TracksTemplates.SilverstoneGpPresent, "FORMULA 1 LENOVO BRITISH GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.RedBullRing, "FORMULA 1 ROLEX GROSSER PREIS VON ÖSTERREICH 2022"),
            new EventTemplate(TracksTemplates.PaulRicard1CV2, "FORMULA 1 LENOVO GRAND PRIX DE FRANCE 2022"),
            new EventTemplate(TracksTemplates.HungaroringPresent, "FORMULA 1 ARAMCO MAGYAR NAGYDÍJ 2022"),
            new EventTemplate(TracksTemplates.SpaPresent, "FORMULA 1 ROLEX BELGIAN GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.ZandvoortGPPresent, "FORMULA 1 HEINEKEN DUTCH GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.MonzaGpPresent, "FORMULA 1 PIRELLI GRAN PREMIO D’ITALIA 2022"),
            new EventTemplate(TracksTemplates.Singapore, "FORMULA 1 SINGAPORE AIRLINES SINGAPORE GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.SuzukaGPPresent, "FORMULA 1 HONDA JAPANESE GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.CotaGP, "FORMULA 1 ARAMCO UNITED STATES GRAND PRIX 2022"),
            new EventTemplate(TracksTemplates.MexicoGpPresent, "FORMULA 1 GRAN PREMIO DE LA CIUDAD DE MÉXICO 2022"),
            new EventTemplate(TracksTemplates.InterlagosGpPresent, "FORMULA 1 HEINEKEN GRANDE PRÊMIO DE SÃO PAULO 2022"),
            new EventTemplate(TracksTemplates.YasMarinaGrandPrix, "FORMULA 1 ETIHAD AIRWAYS ABU DHABI GRAND PRIX 2022"),
        });
    }
}