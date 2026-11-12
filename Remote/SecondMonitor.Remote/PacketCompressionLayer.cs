namespace SecondMonitor.Remote
{
    using System;
    using System.Net;
    using K4os.Compression.LZ4;
    using LiteNetLib.Layers;
    public class PacketCompressionLayer : PacketLayerBase
    {
        private const int extraLayerSize = 2;

        public PacketCompressionLayer() : base(extraLayerSize)
        {
        }

        public override void ProcessInboundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int length)
        {
            var targetLength = BitConverter.ToUInt16(data);
            var target = new byte[targetLength];

            var decodedLength = LZ4Codec.Decode(data, extraLayerSize, length - extraLayerSize, target, 0, target.Length);

            if (decodedLength != targetLength)
            {
                throw new Exception("Decoded data size does not match what was sent!");
            }

            data = target;
            length = decodedLength;
        }

        public override void ProcessOutBoundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int offset, ref int length)
        {
            var target = new byte[LZ4Codec.MaximumOutputSize(length) + extraLayerSize];
            var uncompressedLenght = BitConverter.GetBytes((ushort)length);
            uncompressedLenght.CopyTo(target, 0);

            var encodedLength = LZ4Codec.Encode(data, offset, length, target, extraLayerSize, target.Length - extraLayerSize);
            data = target;
            offset = 0;
            length = encodedLength + extraLayerSize;
        }
    }
}