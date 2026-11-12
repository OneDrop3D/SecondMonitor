namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class OptimalQuantity<T> : IOptimalQuantity where T : class, IQuantity, new()
    {
        [ProtoMember(1)]
        public T ActualQuantity { get; set; } = (T)new T();

        [ProtoMember(2)]
        public T IdealQuantity { get; set; } = (T)new T();

        [ProtoMember(3)]
        public T IdealQuantityWindow { get; set; } = (T)new T();

        public bool Equals(OptimalQuantity<T> other, T tolerance)
        {
            return ActualQuantity.Equals(other.ActualQuantity, tolerance)
                   && IdealQuantity.Equals(other.IdealQuantity, tolerance)
                   && IdealQuantityWindow.Equals(other.IdealQuantityWindow, tolerance);
        }

        public double ActualQuantityRawValue => ActualQuantity.RawValue;
        public double IdealQuantityRawValue => IdealQuantity.RawValue;
        public double IdealQuantityWindowRawValue => IdealQuantityWindow.RawValue;
    }
}