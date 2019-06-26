using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MumbleProto;
using MumbleSharp.Packets;
using MumbleSharp.Services;
using MumbleSharp.Voice.Codecs;

namespace MumbleSharp.Voice.Services
{
    public class VoiceService
    {
        public SpeechCodec TransmissionCodec { get; private set; }

        public VoiceService(MumbleConnection connection)
        {
            connection.RegisterPacketProcessor(new PacketProcessor(PacketType.CodecVersion, ProcessCodecVersionPacket));
        }

        private void ProcessCodecVersionPacket(object packet)
        {
            var codecVersion = (CodecVersion)packet;
            if (codecVersion.Opus)
                TransmissionCodec = SpeechCodec.Opus;
            else if (codecVersion.PreferAlpha)
                TransmissionCodec = SpeechCodec.CeltAlpha;
            else
                TransmissionCodec = SpeechCodec.CeltBeta;
        }

        public void ProcessPackage(byte[] packet, int type)
        {
            var vType = (SpeechCodec)type;
            var target = (SpeechTarget)(packet[0] & 0x1F);

            using (var reader = new UdpPacketReader(new MemoryStream(packet, 1, packet.Length - 1)))
            {
                UInt32 session = (uint)reader.ReadVarInt64();
                Int64 sequence = reader.ReadVarInt64();

                int size = (int)reader.ReadVarInt64();
                size &= 0x1fff;

                if (size == 0)
                    return;

                byte[] data = reader.ReadBytes(size);
                if (data == null)
                    return;

                //OnEncodedVoiceReceived(data, session, sequence, target, vType);
            }
        }
    }
}
