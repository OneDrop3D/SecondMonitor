namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class ClutchDamageInformation : DamageInformation
    {
        public ClutchDamageInformation()
        {
            Temperature = Temperature.Zero;
        }

        [ProtoMember(1, IsRequired = true)]
        public Temperature Temperature { get; set; }
    }
}