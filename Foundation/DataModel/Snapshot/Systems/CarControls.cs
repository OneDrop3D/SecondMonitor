namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;
    using ProtoBuf;

    /// <summary>
    /// Réglages de la voiture modifiables en roulant (répartition de freinage, TC, ABS, cartographie moteur, migration…),
    /// état du moteur et du système hybride, et règlement (limites de piste, pénalités, tour annulé), quand le jeu les donne.
    /// Ajout de SecondMonitor multiplateforme (OneDrop3D) : les champs sont nuls quand le jeu ne les envoie pas.
    /// Les niveaux sont ceux affichés par le jeu (TC 3 sur 11…) ; les maximums permettent de dire « au maximum » ou « coupé ».
    /// </summary>
    [Serializable]
    [ProtoContract]
    public sealed class CarControls
    {
        /// <summary>Vrai si le jeu donne au moins une de ces informations.</summary>
        [ProtoMember(1)]
        public bool Available { get; set; }

        /// <summary>Part du freinage sur l'avant, en % (ex. 54,5).</summary>
        [ProtoMember(2)]
        public double? FrontBrakeBiasPct { get; set; }

        [ProtoMember(3)]
        public int? MotorMap { get; set; }

        [ProtoMember(4)]
        public int? MotorMapMax { get; set; }

        [ProtoMember(5)]
        public int? Tc { get; set; }

        [ProtoMember(6)]
        public int? TcMax { get; set; }

        /// <summary>TC « cut » (coupure de puissance quand le TC agit).</summary>
        [ProtoMember(7)]
        public int? TcCut { get; set; }

        [ProtoMember(8)]
        public int? TcCutMax { get; set; }

        /// <summary>TC « slip » (glissement toléré avant que le TC agisse).</summary>
        [ProtoMember(9)]
        public int? TcSlip { get; set; }

        [ProtoMember(10)]
        public int? TcSlipMax { get; set; }

        [ProtoMember(11)]
        public int? Abs { get; set; }

        [ProtoMember(12)]
        public int? AbsMax { get; set; }

        /// <summary>Migration de la répartition de freinage pendant le freinage (LMU).</summary>
        [ProtoMember(13)]
        public int? Migration { get; set; }

        [ProtoMember(14)]
        public int? MigrationMax { get; set; }

        [ProtoMember(15)]
        public int? FrontAntiRoll { get; set; }

        [ProtoMember(16)]
        public int? FrontAntiRollMax { get; set; }

        [ProtoMember(17)]
        public int? RearAntiRoll { get; set; }

        [ProtoMember(18)]
        public int? RearAntiRollMax { get; set; }

        /// <summary>Récupération d'énergie réglée (kW), LMU.</summary>
        [ProtoMember(19)]
        public double? RegenKw { get; set; }

        /// <summary>Moteur électrique : 0 indisponible, 1 inactif, 2 propulsion (déploiement), 3 récupération.</summary>
        [ProtoMember(20)]
        public int ElectricMotorState { get; set; }

        /// <summary>Couple du moteur électrique (N·m), négatif en récupération.</summary>
        [ProtoMember(21)]
        public double? ElectricMotorTorque { get; set; }

        /// <summary>Charge de la batterie (state of charge), en %.</summary>
        [ProtoMember(22)]
        public double? BatteryPct { get; set; }

        /// <summary>Énergie virtuelle restante (règlement WEC / LMU), en %.</summary>
        [ProtoMember(23)]
        public double? VirtualEnergyPct { get; set; }

        /// <summary>Points de limites de piste accumulés (peut être fractionnaire).</summary>
        [ProtoMember(24)]
        public double? TrackLimitsPoints { get; set; }

        /// <summary>Points de limites de piste qui donnent une pénalité.</summary>
        [ProtoMember(25)]
        public double? TrackLimitsPointsPerPenalty { get; set; }

        /// <summary>Pénalités à purger.</summary>
        [ProtoMember(26)]
        public int? Penalties { get; set; }

        /// <summary>Le jeu a annulé le tour en cours (temps non retenu).</summary>
        [ProtoMember(27)]
        public bool LapInvalidated { get; set; }

        /// <summary>Contact : 0 coupé, 1 contact, 2 contact + démarreur. Nul si le jeu ne le donne pas.</summary>
        [ProtoMember(28)]
        public int? Ignition { get; set; }

        /// <summary>Le pilote a demandé l'arrêt au stand (le jeu attend la voiture).</summary>
        [ProtoMember(29)]
        public bool PitRequested { get; set; }

        /// <summary>Progression du lift and coast demandé par le jeu (0-100), LMU.</summary>
        [ProtoMember(30)]
        public int? LiftAndCoastPct { get; set; }

        /// <summary>Anti-calage activé.</summary>
        [ProtoMember(31)]
        public bool AntiStall { get; set; }

        /// <summary>Limiteur de vitesse disponible sur la voiture.</summary>
        [ProtoMember(32)]
        public bool SpeedLimiterAvailable { get; set; }
    }
}
