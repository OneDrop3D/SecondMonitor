namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;
    using System.Collections.Generic;
    using BasicProperties;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class CarDamageInformation
    {
        public CarDamageInformation()
        {
            Engine = new DamageInformation();
            Transmission = new DamageInformation();
            Suspension = new DamageInformation();
            Bodywork = new DamageInformation();
            Clutch = new ClutchDamageInformation();
        }

        [ProtoMember(1)]
        public DamageInformation Engine { get; set; }

        [ProtoMember(2)]
        public DamageInformation Transmission { get; set; }

        [ProtoMember(3)]
        public DamageInformation Suspension { get; set; }

        [ProtoMember(4)]
        public DamageInformation Bodywork { get; set; }

        [ProtoMember(5)]
        public ClutchDamageInformation Clutch { get; set; }

        /// <summary>
        /// Détail des pièces (aileron avant gauche, fond plat, MGU-K…) quand le jeu le fournit, en plus des quatre totaux.
        /// Champ ajouté par SecondMonitor multiplateforme : les anciens clients l'ignorent (numéro protobuf nouveau).
        /// </summary>
        [ProtoMember(6)]
        public List<DamagePart> Parts { get; set; } = new List<DamagePart>();
    }

    /// <summary>Une pièce endommagée ou usée : identifiant stable, groupe et dégât de 0 à 1.</summary>
    [Serializable]
    [ProtoContract]
    public class DamagePart
    {
        public DamagePart()
        {
        }

        public DamagePart(string key, string group, double damage)
        {
            Key = key;
            Group = group;
            Damage = damage;
        }

        /// <summary>Ex. « front-wing-left », « floor », « engine-mguk-wear », « drs-fault ».</summary>
        [ProtoMember(1)]
        public string Key { get; set; }

        /// <summary>« aero », « engine », « gearbox », « suspension », « brakes », « tyres », « electronics ».</summary>
        [ProtoMember(2)]
        public string Group { get; set; }

        [ProtoMember(3)]
        public double Damage { get; set; }
    }
}