namespace SecondMonitor.Hub.Core.Ai
{
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Ce que l'ingénieur sait de chaque catégorie de voiture (Hypercar LMH et LMDh, LMP2, LMGT3, GTE, GT3, GT4, F1) :
    /// comment elle marche (hybride, énergie virtuelle, pneus), ce qui se règle en roulant et ce que fait chaque réglage,
    /// et quoi proposer pour un symptôme donné avec les valeurs actuelles de la voiture (« TC de 4 à 5 »).
    /// Les réglages réellement disponibles viennent du jeu quand il les donne (ControlsDto) : la base ne propose jamais
    /// un réglage que la voiture n'a pas.
    /// </summary>
    public static class CarKnowledge
    {
        private static readonly string[] Lmh = { "gr010", "toyota", "499p", "ferrari 499", "9x8", "peugeot", "glickenhaus", "scg 007", "vanwall", "isotta", "tipo 6", "valkyrie" };
        private static readonly string[] Lmdh = { "963", "v-series", "cadillac", "m hybrid", "a424", "alpine", "sc63", "lamborghini sc63", "arx-06" };

        /// <summary>Catégorie précise : « Hypercar LMH », « Hypercar LMDh », « Hypercar », « LMP2 », « LMGT3 », « GTE », « GT3 », « GT4 », « F1 », « F2 », « autre ».</summary>
        public static string Category(CarContext ctx)
        {
            string car = (ctx?.CarName ?? string.Empty).ToLowerInvariant();
            if (ctx?.CarClass == "Hypercar")
            {
                return Lmh.Any(car.Contains) ? "Hypercar LMH" : Lmdh.Any(car.Contains) ? "Hypercar LMDh" : "Hypercar";
            }

            if (ctx?.CarClass == "GT3" && ctx.Game == "LMU")
            {
                return "LMGT3";
            }

            if (ctx?.CarClass == "autre" && (car.Contains("gte") || car.Contains("rsr") || car.Contains("c8.r")))
            {
                return "GTE";
            }

            return ctx?.CarClass ?? "autre";
        }

        /// <summary>Comment marche la voiture : quelques phrases pour l'IA (fiche de faits) et pour « comment ça marche ».</summary>
        public static string HowItWorks(string category) => category switch
        {
            "Hypercar LMH" =>
                "Hypercar LMH : moteur thermique sur l'arrière et moteur électrique sur l'avant (quatre roues motrices). " +
                "Règlement WEC : le moteur électrique ne pousse qu'au-dessus d'environ 190 km/h sur le sec ; en dessous, c'est une propulsion. " +
                "Il récupère au freinage sur l'avant. Énergie virtuelle limitée par relais (rechargée à chaque arrêt), carburant à part. " +
                "Pas d'ABS. Réglable en roulant : répartition et migration de freinage, TC (niveau, coupure, glissement selon la voiture), cartographie du moteur électrique (déploiement), récupération (regen).",
            "Hypercar LMDh" =>
                "Hypercar LMDh : moteur thermique et moteur électrique standard de 50 kW, tous deux sur l'essieu arrière (propulsion). " +
                "La récupération freine l'arrière : plus de regen = plus de freinage arrière et un arrière plus vif à l'entrée. " +
                "Énergie virtuelle limitée par relais (rechargée à chaque arrêt), carburant à part. Pas d'ABS. " +
                "Réglable en roulant : répartition et migration de freinage, TC (niveau, coupure, glissement selon la voiture), cartographie du moteur électrique, regen.",
            "Hypercar" =>
                "Hypercar (LMH ou LMDh) : hybride, énergie virtuelle limitée par relais, pas d'ABS. " +
                "Réglable en roulant : répartition et migration de freinage, TC, cartographie du moteur électrique, regen.",
            "LMP2" =>
                "LMP2 (Oreca 07) : prototype sans hybride, V8 Gibson, beaucoup d'appui, pas d'ABS. " +
                "Réglable en roulant : répartition de freinage, TC, et selon le jeu les barres antiroulis. Freinages très courts, l'avant bloque facilement.",
            "LMGT3" =>
                "LMGT3 (GT3 du WEC) : ABS et TC réglables, répartition de freinage, cartographie moteur. Pas d'hybride ; " +
                "énergie virtuelle quand le jeu l'indique. Plus lourde et moins d'appui qu'un prototype : elle se joue au freinage et à la motricité.",
            "GTE" => "GTE : GT de course de l'ancien WEC, ABS et TC réglables, répartition de freinage. Pas d'hybride.",
            "GT3" => "GT3 : ABS et TC réglables (TC et coupure TC sur certaines), répartition de freinage, cartographie moteur. Pas d'hybride.",
            "GT4" => "GT4 : ABS et TC réglables, répartition de freinage. Moins de puissance et d'appui qu'une GT3.",
            "F1" =>
                "F1 : ERS (MGU-K) avec modes de déploiement, différentiel à l'accélération, répartition de freinage, mélange carburant. " +
                "Pas d'ABS ni de TC réglables. Limite de déploiement par tour, récupération au freinage.",
            "F2" => "F2 : pas d'hybride, répartition de freinage et mélange carburant réglables.",
            _ => null,
        };

        /// <summary>Ce que fait chaque réglage en roulant (pour l'IA et pour « c'est quoi la migration ? »).</summary>
        public static readonly IReadOnlyDictionary<string, string> ControlHelp = new Dictionary<string, string>
        {
            ["répartition"] = "Répartition de freinage (% sur l'avant) : vers l'avant = plus stable mais l'avant bloque et sous-vire à l'entrée ; vers l'arrière = la voiture tourne mieux mais l'arrière peut bloquer et partir.",
            ["migration"] = "Migration : déplace la répartition vers l'arrière quand on relâche le frein en entrée de virage. Plus de migration = la voiture tourne mieux en entrée, arrière plus vif.",
            ["tc"] = "TC : plus haut = moins de patinage et un arrière plus stable en sortie ; plus bas = plus de motricité mais les pneus arrière s'usent et chauffent.",
            ["coupure"] = "Coupure TC : combien de puissance le TC retire quand il agit. Plus haut = sortie plus douce, plus bas = plus de puissance gardée.",
            ["glissement"] = "Glissement TC : patinage toléré avant que le TC agisse. Plus bas = le TC agit plus tôt.",
            ["abs"] = "ABS : plus haut = moins de blocage mais freinage un peu plus long ; plus bas = freinage plus court si on dose bien.",
            ["cartographie"] = "Cartographie : puissance contre consommation. Sur une hypercar, c'est le déploiement du moteur électrique : plus agressive = plus de puissance mais plus d'énergie virtuelle consommée.",
            ["regen"] = "Regen : plus haut = plus d'énergie récupérée au freinage, mais le moteur électrique freine aussi (l'arrière sur une LMDh, l'avant sur une LMH) et change l'équilibre au freinage.",
            ["antiroulis"] = "Barres antiroulis : plus dure à l'avant = plus de sous-virage ; plus dure à l'arrière = plus de survirage.",
        };

        /// <summary>Analyse d'une usure brutale : cause probable et réglage à proposer avec les valeurs actuelles (null si rien ne se règle en roulant).</summary>
        public static (string Cause, string Proposal) AnalyseWear(WearSpike s, ControlsDto c, CarContext ctx)
        {
            bool rear = s.Wheel is "RL" or "RR";
            bool hasAbs = c?.AbsMax > 0 || (c == null && ctx?.HasAbs == true);
            if (s.BothAxles && s.Phase is "milieu" or "ligne droite")
            {
                return ("les quatre pneus ont glissé ensemble, sans doute une glissade ou un passage hors piste", null);
            }

            return s.Phase switch
            {
                "sortie" when rear => ("patinage à la réaccélération", TcUp(c, ctx) ?? "dosez l'accélérateur plus progressivement"),
                "sortie" => ("l'avant glisse à la réaccélération", "remettez les gaz plus tard, roues plus droites"),
                "freinage" or "entrée" when !rear => ("blocage de l'avant au freinage", BiasBack(c) ?? (hasAbs ? AbsUp(c) : null) ?? "freinez un peu plus tôt et relâchez progressivement"),
                "freinage" or "entrée" => ("l'arrière bloque ou glisse au freinage", BiasForward(c, Category(ctx)) ?? "freinez plus droit avant de tourner"),
                "milieu" when rear => ("survirage au milieu du virage", c?.RearAntiRollMax > 0 && c.RearAntiRoll > 1 ? $"barre antiroulis arrière de {c.RearAntiRoll} à {c.RearAntiRoll - 1}" : "moins d'angle de volant, laissez rouler"),
                "milieu" => ("sous-virage au milieu du virage, l'avant frotte", c?.FrontAntiRollMax > 0 && c.FrontAntiRoll > 1 ? $"barre antiroulis avant de {c.FrontAntiRoll} à {c.FrontAntiRoll - 1}" : "moins de vitesse d'entrée et moins de volant"),
                _ => ("glissade en ligne droite ou passage sur un vibreur", null),
            };
        }

        public static string TcUp(ControlsDto c, CarContext ctx)
        {
            if (c?.Tc is int tc && c.TcMax is int max)
            {
                if (tc < max)
                {
                    return $"TC de {tc} à {tc + 1}";
                }

                return c.TcCut is int cut && c.TcCutMax is int cutMax && cut < cutMax
                    ? $"TC déjà au maximum : coupure TC de {cut} à {cut + 1}"
                    : "TC déjà au maximum : dosez l'accélérateur";
            }

            return ctx?.HasTc == true ? "un cran de TC en plus" : null;
        }

        public static string BiasBack(ControlsDto c) =>
            c?.FrontBrakeBiasPct is double b ? $"répartition de {N(b, 1)} à {N(b - 0.5, 1)} %" : null;

        public static string BiasForward(ControlsDto c, string category)
        {
            if (category == "Hypercar LMDh" && c?.RegenKw is double regen && regen > 0)
            {
                return c.FrontBrakeBiasPct is double b0 ? $"répartition de {N(b0, 1)} à {N(b0 + 0.5, 1)} %, ou un peu moins de regen" : "un peu moins de regen";
            }

            if (c?.Migration is int mig && mig > 0 && c.MigrationMax > 0)
            {
                return $"migration de {mig} à {mig - 1}";
            }

            return c?.FrontBrakeBiasPct is double b ? $"répartition de {N(b, 1)} à {N(b + 0.5, 1)} %" : null;
        }

        public static string AbsUp(ControlsDto c) =>
            c?.Abs is int a && c.AbsMax is int max && a < max ? $"ABS de {a} à {a + 1}" : null;

        /// <summary>Réglages actuels en une phrase : « Répartition 54,5 %, migration 3, TC 4 sur 11, coupure TC 2, cartographie 2 sur 4. »</summary>
        public static string CurrentSettings(ControlsDto c)
        {
            if (c == null)
            {
                return null;
            }

            var parts = new List<string>();
            if (c.FrontBrakeBiasPct is double b)
            {
                parts.Add($"répartition {N(b, 1)} % avant");
            }

            if (c.Migration is int mig)
            {
                parts.Add($"migration {mig}");
            }

            if (c.Tc is int tc)
            {
                parts.Add(tc == 0 ? "TC coupé" : $"TC {tc}{(c.TcMax is int tm ? $" sur {tm}" : string.Empty)}");
            }

            if (c.TcCut is int cut)
            {
                parts.Add($"coupure TC {cut}");
            }

            if (c.TcSlip is int slip)
            {
                parts.Add($"glissement TC {slip}");
            }

            if (c.Abs is int abs)
            {
                parts.Add(abs == 0 ? "ABS coupé" : $"ABS {abs}");
            }

            if (c.MotorMap is int map)
            {
                parts.Add($"cartographie {map}{(c.MotorMapMax is int mm ? $" sur {mm}" : string.Empty)}");
            }

            if (c.RegenKw is double regen && regen > 0)
            {
                parts.Add($"regen {N(regen, 0)} kW");
            }

            return parts.Count == 0 ? null : Cap(string.Join(", ", parts)) + ".";
        }

        /// <summary>État de l'énergie : moteur électrique, batterie, énergie virtuelle, cartographie, regen.</summary>
        public static string EnergyState(ControlsDto c, HybridDto hybrid)
        {
            var parts = new List<string>();
            if (c?.ElectricMotor is string motor)
            {
                parts.Add($"moteur électrique en {motor}");
            }

            if ((c?.BatteryPct ?? hybrid?.ChargePct) is double bat)
            {
                parts.Add($"batterie {N(bat, 0)} %");
            }

            if (c?.VirtualEnergyPct is double ve)
            {
                parts.Add($"énergie virtuelle {N(ve, 0)} %");
            }

            if (c?.MotorMap is int map)
            {
                parts.Add($"cartographie {map}{(c.MotorMapMax is int mm ? $" sur {mm}" : string.Empty)}");
            }

            if (c?.RegenKw is double regen && regen > 0)
            {
                parts.Add($"regen {N(regen, 0)} kW");
            }

            return parts.Count == 0 ? null : Cap(string.Join(", ", parts)) + ".";
        }
    }
}
