"""
Catalogue des circuits de la bibliothèque de SecondMonitor (lu par circuits.py).

Pour chaque circuit : identifiant, nom, ville, longueur officielle (m), alias (noms et morceaux de noms envoyés par les jeux,
comparés sans accents ni casse, « _ » = espace), et pour les tracés OpenStreetMap : centre de la recherche (lat, lon),
rayon (m), sens de course connu (« cw » horaire / « ccw » anti-horaire, vu du ciel, nord en haut) servant de secours
quand les voies ne sont pas balisées oneway, et contraintes de tracé (require / avoid : noms de voies OSM).

Les variantes de tracé (Indy, National, Short…) sont des entrées à part avec le mot de variante dans leurs alias :
TrackLibrary ne prend jamais un tracé si le jeu annonce un mot de variante que le tracé n'a pas, ni si la longueur diffère.
"""

# --- Tracés bacinger/f1-circuits (MIT) : alias supplémentaires vus dans les jeux ---------------------------------------
BACINGER_ALIASES = {
    "au-1953": ["melbourne grand prix circuit", "albert park", "melbourne"],
    "bh-2002": ["bahrain international circuit", "bahrain", "sakhir", "bahrain gp", "bahrain grand prix"],
    "cn-2004": ["shanghai", "shanghai circuit", "shanghai international"],
    "es-1991": ["barcelona", "catalunya", "montmelo", "barcelona catalunya", "circuit de catalunya"],
    "mc-1929": ["monaco", "monte carlo"],
    "ca-1978": ["gilles villeneuve", "montreal", "circuit gilles villeneuve"],
    "fr-1969": ["paul ricard", "le castellet", "circuit paul ricard", "paul ricard 1a"],
    "at-1969": ["red bull ring", "redbull ring", "spielberg", "osterreichring", "red bull ring spielberg", "rbr"],
    "gb-1948": ["silverstone", "silverstone circuit", "silverstone arena", "silverstone gp", "silverstone 2019", "silverstone 2020"],
    "de-1932": ["hockenheim", "hockenheimring", "hockenheim gp"],
    "hu-1986": ["hungaroring", "budapest", "mogyorod"],
    "be-1925": ["spa", "spa francorchamps", "francorchamps", "circuit de spa francorchamps", "spa 2022"],
    "it-1922": ["monza", "autodromo nazionale monza", "monza circuit"],
    "sg-2008": ["marina bay", "singapore"],
    "ru-2014": ["sochi"],
    "jp-1962": ["suzuka", "suzuka circuit", "suzuka international racing course"],
    "us-2012": ["circuit of the americas", "cota", "austin", "americas"],
    "mx-1962": ["hermanos rodriguez", "mexico", "mexico city", "autodromo hermanos rodriguez"],
    "br-1940": ["interlagos", "jose carlos pace", "sao paulo", "autodromo jose carlos pace"],
    "ae-2009": ["yas marina", "abu dhabi", "yas marina circuit"],
    "it-1953": ["imola", "enzo e dino ferrari", "autodromo enzo e dino ferrari", "santerno"],
    "de-1927": ["nurburgring gp", "nurburgring", "nuerburgring", "nurburgring grand prix", "nurburgring gp strecke"],
    "pt-2008": ["algarve", "portimao", "algarve international circuit", "autodromo internacional do algarve"],
    "it-1914": ["mugello"],
    "my-1999": ["sepang", "sepang international circuit"],
    "tr-2005": ["istanbul", "istanbul park"],
    "nl-1948": ["zandvoort", "circuit zandvoort", "cm com circuit zandvoort"],
    "fr-1960": ["magny cours", "nevers", "nevers magny cours"],
    "pt-1972": ["estoril", "autodromo do estoril"],
    "br-1977": ["brasilia", "nelson piquet"],
    "sa-2021": ["jeddah", "jeddah corniche"],
    "us-2022": ["miami", "miami international autodrome"],
    "qa-2004": ["losail", "lusail", "qatar", "lusail international circuit"],
    "es-2026": ["madring", "madrid"],
    "az-2016": ["baku", "baku city circuit"],
    "us-2023": ["las vegas", "las vegas strip"],
    "us-1909": ["indianapolis", "indianapolis motor speedway", "ims"],
    "ar-1952": ["buenos aires", "oscar y juan galvez", "oscar galvez"],
    "za-1961": ["kyalami", "kyalami grand prix circuit"],
    "us-1956": ["watkins glen", "watkins glen international", "the glen"],
}

# Sens de course connus des tracés bacinger (contrôle automatique : voir circuits.py --check).
BACINGER_DIRECTION = {
    "au-1953": "cw", "bh-2002": "cw", "cn-2004": "cw", "es-1991": "cw", "mc-1929": "cw", "ca-1978": "cw",
    "fr-1969": "cw", "at-1969": "cw", "gb-1948": "cw", "de-1932": "cw", "hu-1986": "cw", "be-1925": "cw",
    "it-1922": "cw", "sg-2008": "ccw", "ru-2014": "cw", "us-2012": "ccw", "mx-1962": "cw", "br-1940": "ccw",
    "ae-2009": "ccw", "it-1953": "ccw", "de-1927": "cw", "pt-2008": "cw", "it-1914": "cw", "my-1999": "cw",
    "tr-2005": "ccw", "nl-1948": "cw", "fr-1960": "cw", "pt-1972": "cw", "sa-2021": "ccw", "us-2022": "ccw",
    "qa-2004": "cw", "az-2016": "ccw", "us-2023": "ccw", "us-1956": "cw",
}


def C(id, name, length, lat, lon, aliases, location="", dir=None, radius=2500, group=None, require=None, avoid=None, roads=None, games="", public_roads=False, snap=12.0, max_gap=1500):
    """public_roads : circuit en partie sur la voie publique (les trous entre voies raceway sont comblés par la route)."""
    return dict(id=id, name=name, length=length, lat=lat, lon=lon, aliases=aliases, location=location, dir=dir,
                radius=radius, group=group or id, require=require, avoid=avoid, roads=roads, games=games, public_roads=public_roads, snap=snap, max_gap=max_gap)


# --- Tracés OpenStreetMap (ODbL) ----------------------------------------------------------------------------------------
OSM = [
    # Royaume-Uni
    C("gb-brands-hatch-gp", "Brands Hatch Grand Prix", 3908, 51.3569, 0.2628, ["brands hatch", "brands hatch circuit", "brands hatch gp", "brands hatch grand prix"], "West Kingsdown", "cw", group="brands-hatch", games="ACC AC AMS2 R3E rF2 iRacing PC2"),
    C("gb-brands-hatch-indy", "Brands Hatch Indy", 1929, 51.3569, 0.2628, ["brands hatch indy", "indy"], "West Kingsdown", "cw", group="brands-hatch", games="AC AMS2 R3E rF2 iRacing PC2"),
    C("gb-donington-gp", "Donington Park Grand Prix", 4020, 52.8306, -1.3750, ["donington", "donington park", "donington gp", "donington park grand prix"], "Castle Donington", "cw", group="donington", games="ACC AMS2 R3E iRacing PC2"),
    C("gb-donington-national", "Donington Park National", 3149, 52.8306, -1.3750, ["donington national", "donington park national", "national"], "Castle Donington", "cw", group="donington", games="AMS2 R3E iRacing PC2"),
    C("gb-oulton-international", "Oulton Park International", 4307, 53.1786, -2.6136, ["oulton park", "oulton", "oulton park international"], "Little Budworth", group="oulton", games="ACC AMS2 iRacing PC2"),
    C("gb-oulton-island", "Oulton Park Island", 3621, 53.1786, -2.6136, ["oulton park island", "island"], "Little Budworth", group="oulton", games="AMS2 iRacing PC2"),
    C("gb-oulton-fosters", "Oulton Park Fosters", 2690, 53.1786, -2.6136, ["oulton park fosters", "fosters"], "Little Budworth", group="oulton", games="AMS2 iRacing PC2"),
    C("gb-snetterton-300", "Snetterton 300", 4778, 52.4650, 0.9460, ["snetterton", "snetterton 300", "snetterton circuit", "300 circuit", "300"], "Snetterton", group="snetterton", games="ACC AMS2 iRacing PC2"),
    C("gb-snetterton-200", "Snetterton 200", 3213, 52.4650, 0.9460, ["snetterton 200", "200 circuit", "200"], "Snetterton", group="snetterton", games="AMS2 iRacing PC2"),
    C("gb-silverstone-national", "Silverstone National", 2638, 52.0733, -1.0147, ["silverstone national", "national"], "Silverstone", "cw", radius=3000, group="silverstone", games="AC AMS2 R3E iRacing PC2"),
    C("gb-silverstone-international", "Silverstone International", 2979, 52.0733, -1.0147, ["silverstone international", "international"], "Silverstone", "cw", radius=3000, group="silverstone", games="AC AMS2 R3E iRacing PC2"),
    C("gb-knockhill", "Knockhill Racing Circuit", 2045, 56.1308, -3.5072, ["knockhill", "knockhill racing circuit"], "Dunfermline", group="knockhill", games="R3E PC2 AC"),
    C("gb-thruxton", "Thruxton Circuit", 3792, 51.2096, -1.6075, ["thruxton"], "Andover", "cw", games="iRacing AC"),
    C("gb-croft", "Croft Circuit", 3423, 54.4555, -1.5570, ["croft", "croft circuit"], "Dalton-on-Tees", games="PC2 AC"),
    C("gb-cadwell-park", "Cadwell Park", 3486, 53.3100, -0.0615, ["cadwell", "cadwell park", "cadwell grand prix"], "Louth", games="AMS2 PC2 AC"),
    C("gb-goodwood", "Goodwood Circuit", 3809, 50.8592, -0.7590, ["goodwood", "goodwood circuit", "goodwood motor circuit"], "Chichester", "cw", games="AMS2 AC"),

    # France
    C("fr-le-mans", "Circuit des 24 Heures du Mans", 13626, 47.9300, 0.2150, ["circuit de la sarthe", "le mans", "sarthe", "circuit des 24 heures du mans", "24 heures du mans", "la sarthe", "le mans 24h"], "Le Mans", "cw", radius=6500, group="le-mans", public_roads=True, snap=600, max_gap=3500, games="LMU rF2 AMS2 PC2 iRacing AC R3E"),
    C("fr-le-mans-bugatti", "Circuit Bugatti", 4185, 47.9530, 0.2140, ["bugatti", "le circuit bugatti", "circuit bugatti", "le mans bugatti"], "Le Mans", "cw", group="le-mans-bugatti", games="PC2 AC"),
    C("fr-dijon", "Dijon-Prenois", 3801, 47.3624, 4.8995, ["dijon", "dijon prenois"], "Prenois", "cw", games="AMS2 AC"),
    C("fr-charade", "Circuit de Charade", 3975, 45.7421, 3.0314, ["charade", "circuit de charade", "clermont ferrand"], "Clermont-Ferrand", games="AC AMS2"),
    C("fr-ledenon", "Circuit de Lédenon", 3150, 43.9240, 4.5055, ["ledenon", "circuit de ledenon"], "Lédenon", games="AC"),
    C("fr-nogaro", "Circuit Paul Armagnac", 3636, 43.7690, -0.0380, ["nogaro", "paul armagnac", "circuit paul armagnac"], "Nogaro", games="AC"),

    # Benelux
    C("be-zolder", "Circuit Zolder", 4011, 50.9900, 5.2570, ["zolder", "circuit zolder", "terlamen"], "Heusden-Zolder", games="ACC AC R3E PC2 iRacing"),
    C("nl-assen", "TT Circuit Assen", 4542, 52.9617, 6.5236, ["assen", "tt circuit assen"], "Assen", "cw", games="R3E AC"),

    # Allemagne
    C("de-hockenheim-national", "Hockenheimring National", 3692, 49.3278, 8.5656, ["hockenheim national", "hockenheimring national", "national"], "Hockenheim", "cw", group="hockenheim", games="R3E PC2 AMS2 iRacing"),
    C("de-hockenheim-short", "Hockenheimring Short", 2600, 49.3278, 8.5656, ["hockenheim short", "hockenheimring short", "short"], "Hockenheim", "cw", group="hockenheim", games="R3E PC2 AMS2"),
    C("de-nurburgring-sprint", "Nürburgring Sprint", 3629, 50.3356, 6.9475, ["nurburgring sprint", "nurburgring", "nuerburgring", "sprint"], "Nürburg", "cw", group="nurburgring-gp", avoid=["Mercedes"], games="AC PC2 R3E AMS2"),
    C("de-nordschleife", "Nürburgring Nordschleife", 20832, 50.3460, 6.9630, ["nordschleife", "nurburgring nordschleife", "nordschleife full", "nordschleife tourist", "nurburgring combined", "touristenfahrten"], "Nürburg", "cw", radius=7500, group="nordschleife", avoid=["Mercedes", "Müllenbach", "Mullenbach", "Castrol", "Michelin", "Dunlop", "Yokohama"], games="AC R3E PC2 AMS2 rF2 iRacing"),
    C("de-nurburgring-24h", "Nürburgring 24h (Nordschleife + GP)", 25378, 50.3460, 6.9630, ["nurburgring 24h", "nurburgring 24", "24h", "24 hour", "nordschleife 24h", "nordschleife endurance", "endurance", "gesamtstrecke", "nurburgring combined", "nordschleife"], "Nürburg", "cw", radius=7500, group="nordschleife", games="ACC AC R3E PC2 AMS2 iRacing"),
    C("de-sachsenring", "Sachsenring", 3645, 50.7917, 12.6883, ["sachsenring"], "Hohenstein-Ernstthal", "ccw", games="R3E AC"),
    C("de-norisring", "Norisring", 2300, 49.4318, 11.1240, ["norisring", "nuremberg", "nurnberg"], "Nuremberg", roads="Norisring|Schöne Aussicht|Große Straße|Grundig|Dutzendteich|Zeppelin", games="R3E rF2 AC"),
    C("de-lausitzring", "Lausitzring", 3478, 51.5333, 13.9294, ["lausitzring", "lausitz", "dekra lausitzring", "eurospeedway"], "Klettwitz", games="R3E PC2 AC"),
    C("de-oschersleben", "Motorsport Arena Oschersleben", 3696, 52.0270, 11.2775, ["oschersleben", "motorsport arena oschersleben", "magdeburg"], "Oschersleben", "cw", games="R3E PC2 AC"),
    C("de-bilster-berg", "Bilster Berg", 4200, 51.7944, 9.0628, ["bilster berg", "bilsterberg"], "Bad Driburg", games="R3E AC"),

    # Autriche, Europe centrale
    C("at-red-bull-ring-national", "Red Bull Ring National", 2336, 47.2197, 14.7647, ["red bull ring national", "national", "spielberg national"], "Spielberg", "cw", group="red-bull-ring", games="AC PC2"),
    C("at-salzburgring", "Salzburgring", 4241, 47.8230, 13.1700, ["salzburgring", "salzburg"], "Salzbourg", games="R3E PC2 AC"),
    C("cz-brno", "Masaryk Circuit (Brno)", 5403, 49.2035, 16.4446, ["brno", "masaryk", "automotodrom brno", "masarykuv okruh"], "Brno", "cw", games="R3E PC2 AC"),
    C("cz-most", "Autodrom Most", 4212, 50.5194, 13.6064, ["most", "autodrom most"], "Most", games="R3E AC"),
    C("sk-slovakia-ring", "Slovakia Ring", 5922, 48.0541, 17.5650, ["slovakia ring", "slovakiaring", "orechova poton"], "Orechová Potôň", games="R3E AC"),

    # Italie, Espagne
    C("it-misano", "Misano World Circuit Marco Simoncelli", 4226, 43.9615, 12.6845, ["misano", "misano world circuit", "marco simoncelli"], "Misano Adriatico", "cw", games="ACC AC iRacing"),
    C("it-vallelunga", "Vallelunga Piero Taruffi", 4085, 42.1585, 12.3700, ["vallelunga", "piero taruffi"], "Campagnano di Roma", "cw", games="AC AMS2 rF2"),
    C("it-magione", "Autodromo dell'Umbria (Magione)", 2507, 43.1350, 12.2130, ["magione", "autodromo dell umbria", "mario umberto borzacchini"], "Magione", games="AC"),
    C("es-jerez", "Circuito de Jerez", 4428, 36.7085, -6.0340, ["jerez", "circuito de jerez", "angel nieto"], "Jerez de la Frontera", "cw", games="AMS2 AC iRacing"),
    C("es-valencia", "Circuit Ricardo Tormo", 4005, 39.4855, -0.6285, ["valencia", "ricardo tormo", "cheste", "circuit ricardo tormo"], "Cheste", "ccw", games="ACC AC"),
    C("es-aragon", "MotorLand Aragón", 5345, 41.0790, -0.2030, ["aragon", "motorland", "motorland aragon", "alcaniz"], "Alcañiz", "ccw", games="AC iRacing"),
    C("es-navarra", "Circuito de Navarra", 3933, 42.5600, -2.1710, ["navarra", "circuito de navarra", "los arcos"], "Los Arcos", games="iRacing AC"),

    # Scandinavie
    C("se-anderstorp", "Anderstorp Raceway", 4025, 57.2643, 13.6018, ["anderstorp", "scandinavian raceway", "anderstorp raceway"], "Anderstorp", games="R3E AC"),
    C("se-mantorp", "Mantorp Park", 3106, 58.3700, 15.2850, ["mantorp", "mantorp park"], "Mantorp", games="R3E AC"),
    C("se-karlskoga", "Gelleråsen Arena (Karlskoga)", 2400, 59.3825, 14.5144, ["karlskoga", "gellerasen", "gellerasen arena", "karlskoga motorstadion"], "Karlskoga", games="R3E AC"),
    C("se-knutstorp", "Ring Knutstorp", 2067, 55.9860, 13.1070, ["knutstorp", "ring knutstorp"], "Kågeröd", games="R3E AC"),
    C("se-falkenberg", "Falkenbergs Motorbana", 2650, 56.9755, 12.5680, ["falkenberg", "falkenbergs motorbana"], "Falkenberg", games="R3E AC"),
    C("no-rudskogen", "Rudskogen Motorsenter", 3254, 59.3700, 11.2600, ["rudskogen", "rudskogen motorsenter"], "Rakkestad", games="iRacing AC"),
    C("dk-jyllandsringen", "Jyllandsringen", 2300, 56.1770, 9.6580, ["jyllandsringen", "jyllands ringen"], "Silkeborg", games="R3E AC"),

    # Russie, Moyen-Orient
    C("ru-moscow-raceway", "Moscow Raceway", 3955, 55.9954, 36.2654, ["moscow raceway", "moscow"], "Volokolamsk", games="R3E AC"),
    C("ae-dubai", "Dubai Autodrome", 5390, 25.0505, 55.2385, ["dubai", "dubai autodrome", "dubai grand prix"], "Dubaï", group="dubai", games="R3E PC2 AC"),
    C("bh-outer", "Bahrain Outer Circuit", 3543, 26.0325, 50.5106, ["bahrain outer", "outer", "bahrain outer circuit", "short", "sakhir outer"], "Sakhir", "cw", group="bahrain", games="LMU F1 rF2 iRacing"),
    C("bh-endurance", "Bahrain Endurance Circuit", 6299, 26.0325, 50.5106, ["bahrain endurance", "endurance", "bahrain endurance circuit"], "Sakhir", "cw", group="bahrain", games="LMU rF2"),

    # Asie
    C("jp-fuji", "Fuji Speedway", 4563, 35.3717, 138.9275, ["fuji", "fuji speedway", "fuji international speedway"], "Oyama", "cw", games="LMU rF2 AMS2 PC2 iRacing AC"),
    C("jp-okayama", "Okayama International Circuit", 3703, 34.9150, 134.2210, ["okayama", "okayama international", "aida", "ti circuit aida"], "Mimasaka", "cw", games="AMS2 iRacing AC"),
    C("jp-motegi", "Mobility Resort Motegi (circuit routier)", 4801, 36.5322, 140.2275, ["motegi", "twin ring motegi", "mobility resort motegi"], "Motegi", avoid=["oval"], games="iRacing R3E AC"),
    C("jp-sugo", "Sportsland Sugo", 3704, 38.1410, 140.7770, ["sugo", "sportsland sugo"], "Murata", games="PC2 AC"),
    C("jp-autopolis", "Autopolis", 4674, 33.0420, 131.0590, ["autopolis"], "Hita", games="PC2 AC"),
    C("jp-tsukuba", "Tsukuba Circuit", 2045, 36.1503, 139.9195, ["tsukuba", "tsukuba circuit", "tsukuba 2000"], "Shimotsuma", "cw", games="iRacing AC"),
    C("cn-zhuhai", "Zhuhai International Circuit", 4319, 22.3710, 113.5610, ["zhuhai", "zhuhai international circuit", "zhuhai international"], "Zhuhai", "cw", games="R3E PC2 AC"),
    C("th-chang", "Chang International Circuit", 4554, 14.9584, 103.0848, ["chang", "chang international circuit", "buriram"], "Buriram", "cw", games="R3E PC2 AC"),

    C("nz-ruapuna", "Ruapuna Park", 3330, -43.5326, 172.4823, ["ruapuna", "ruapuna park", "mike pero motorsport park"], "Christchurch", games="PC2 AC"),
    C("au-winton", "Winton Motor Raceway", 3000, -36.5184, 146.0871, ["winton", "winton motor raceway"], "Benalla", games="iRacing AC"),

    # Océanie
    C("au-bathurst", "Mount Panorama Circuit", 6213, -33.4455, 149.5573, ["mount panorama", "bathurst", "mount panorama circuit", "bathurst circuit"], "Bathurst", radius=3000, roads="^(Mountain Straight|Conrod Straight|Pit Straight|Panorama Avenue|Mount Panorama|Pit Lane)$", snap=300, games="ACC rF2 AMS2 R3E PC2 iRacing AC"),
    C("au-phillip-island", "Phillip Island Grand Prix Circuit", 4445, -38.5010, 145.2330, ["phillip island", "phillip island grand prix circuit"], "Ventnor", games="iRacing AC"),
    C("au-sandown", "Sandown Raceway", 3104, -37.9497, 145.1675, ["sandown", "sandown raceway"], "Melbourne", games="iRacing AC"),

    # Amériques
    C("us-laguna-seca", "WeatherTech Raceway Laguna Seca", 3602, 36.5843, -121.7535, ["laguna seca", "mazda raceway", "weathertech raceway", "mazda raceway laguna seca", "weathertech raceway laguna seca"], "Monterey", "ccw", games="ACC AC AMS2 R3E rF2 iRacing PC2"),
    C("us-sonoma", "Sonoma Raceway", 4060, 38.1615, -122.4547, ["sonoma", "sonoma raceway", "sears point", "infineon"], "Sonoma", group="sonoma", games="R3E PC2 iRacing AC"),
    C("us-road-america", "Road America", 6515, 43.7980, -87.9895, ["road america", "elkhart lake"], "Elkhart Lake", "cw", radius=3000, games="R3E AMS2 PC2 iRacing AC rF2"),
    C("us-road-atlanta", "Michelin Raceway Road Atlanta", 4088, 34.1491, -83.8156, ["road atlanta", "michelin raceway", "michelin raceway road atlanta"], "Braselton", "cw", games="AMS2 iRacing AC"),
    C("us-sebring", "Sebring International Raceway", 6019, 27.4544, -81.3484, ["sebring", "sebring international raceway", "sebring international"], "Sebring", "cw", radius=3000, avoid=["Runway", "Taxiway"], games="LMU rF2 AMS2 iRacing AC"),
    C("us-daytona-road", "Daytona International Speedway (circuit routier)", 5729, 29.1852, -81.0705, ["daytona", "daytona international speedway", "daytona road course", "road course", "daytona rc"], "Daytona Beach", "ccw", radius=2000, group="daytona", games="AMS2 iRacing PC2 rF2 AC"),
    C("us-daytona-oval", "Daytona International Speedway (ovale)", 4023, 29.1852, -81.0705, ["daytona oval", "tri oval", "oval", "daytona tri oval", "daytona international speedway"], "Daytona Beach", "ccw", radius=2000, group="daytona", games="AMS2 iRacing PC2 rF2"),
    C("us-indianapolis-road", "Indianapolis Motor Speedway (circuit routier)", 3925, 39.7950, -86.2347, ["indianapolis", "indianapolis motor speedway", "ims", "indianapolis road course", "road course", "indy road course", "indianapolis 2014"], "Indianapolis", "cw", radius=2000, group="indianapolis", games="ACC AMS2 iRacing rF2 R3E"),
    C("us-indianapolis-oval", "Indianapolis Motor Speedway (ovale)", 4023, 39.7950, -86.2347, ["indianapolis oval", "oval", "indianapolis", "indianapolis motor speedway", "ims oval", "indy 500"], "Indianapolis", "ccw", radius=2000, group="indianapolis", games="AMS2 iRacing rF2 R3E"),
    C("us-watkins-glen-short", "Watkins Glen International (court)", 3943, 42.3369, -76.9272, ["watkins glen short", "short", "watkins glen", "short circuit", "classic"], "Watkins Glen", "cw", group="watkins-glen", games="PC2 iRacing AMS2"),
    C("us-lime-rock", "Lime Rock Park", 2470, 41.9284, -73.3818, ["lime rock", "lime rock park"], "Lakeville", "cw", games="iRacing rF2 AC"),
    C("us-mid-ohio", "Mid-Ohio Sports Car Course", 3634, 40.6894, -82.6366, ["mid ohio", "mid ohio sports car course", "midohio"], "Lexington", "cw", games="R3E iRacing AC"),
    C("us-barber", "Barber Motorsports Park", 3830, 33.5306, -86.6194, ["barber", "barber motorsports park"], "Birmingham", games="iRacing AC"),
    C("us-vir", "Virginia International Raceway", 5263, 36.5667, -79.2061, ["virginia international raceway", "vir", "virginia"], "Alton", "cw", group="vir", games="AMS2 iRacing AC"),
    C("us-summit-point", "Summit Point Raceway", 3218, 39.2368, -77.9719, ["summit point", "summit point raceway"], "Summit Point", games="iRacing AC"),
    C("us-willow-springs", "Willow Springs International Raceway", 4023, 34.8710, -118.2629, ["willow springs", "willow springs international raceway", "big willow", "international raceway"], "Rosamond", games="PC2 AC"),
    C("us-cota-national", "Circuit of the Americas National", 3702, 30.1328, -97.6411, ["cota national", "circuit of the americas national", "national", "short", "circuit of the americas", "cota"], "Austin", "ccw", group="cota", games="F1 PC2 LMU AMS2"),
    C("ca-mosport", "Canadian Tire Motorsport Park", 3957, 44.0532, -78.6763, ["canadian tire motorsport park", "mosport", "ctmp", "canadian tire"], "Bowmanville", "cw", games="R3E AMS2 iRacing AC"),
    C("ar-termas", "Autódromo Termas de Río Hondo", 4806, -27.5066, -64.9135, ["termas de rio hondo", "termas", "rio hondo"], "Termas de Río Hondo", games="AC"),

    # Brésil (Automobilista 2)
    C("br-curitiba", "Autódromo Internacional de Curitiba", 3695, -25.4460, -49.1960, ["curitiba", "autodromo internacional de curitiba", "pinhais"], "Pinhais", games="AMS2"),
    C("br-cascavel", "Autódromo Zilmar Beux (Cascavel)", 3058, -24.9814, -53.3812, ["cascavel", "zilmar beux"], "Cascavel", games="AMS2"),
    C("br-goiania", "Autódromo Internacional Ayrton Senna (Goiânia)", 3835, -16.7190, -49.1930, ["goiania", "autodromo de goiania", "ayrton senna goiania"], "Goiânia", games="AMS2"),
    # C("br-velocitta" : position inconnue de Nominatim, à compléter
    # C("br-velocitta", "Velocittà", 3443, -22.3080, -46.8750, ["velocitta", "velo citta", "mogi guacu"], "Mogi Guaçu", games="AMS2"),
    C("br-taruma", "Autódromo Internacional de Tarumã", 3065, -30.0530, -51.0160, ["taruma", "autodromo de taruma"], "Viamão", games="AMS2"),
    C("br-santa-cruz", "Autódromo Internacional de Santa Cruz do Sul", 3531, -29.8003, -52.4363, ["santa cruz do sul", "santa cruz"], "Santa Cruz do Sul", games="AMS2"),
    C("br-londrina", "Autódromo Internacional Ayrton Senna (Londrina)", 3145, -23.2830, -51.1660, ["londrina", "ayrton senna londrina"], "Londrina", games="AMS2"),
    C("br-guapore", "Autódromo Internacional de Guaporé", 3080, -28.8470, -51.8555, ["guapore", "autodromo de guapore"], "Guaporé", games="AMS2"),
    C("br-velopark", "Velopark", 2278, -29.8244, -51.3225, ["velopark"], "Nova Santa Rita", games="AMS2"),
]
