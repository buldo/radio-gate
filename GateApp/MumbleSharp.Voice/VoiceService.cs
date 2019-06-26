using System;
using System.Collections.Concurrent;
using System.IO;
using MumbleProto;
using MumbleSharp.Packets;
using MumbleSharp.Services;
using MumbleSharp.Services.UsersManagement;
using MumbleSharp.Voice.Codecs;

namespace MumbleSharp.Voice
{
    public class VoiceService
    {
        private readonly MumbleConnection _connection;
        private readonly UsersManagementService _usersManagementService;
        private readonly ConcurrentDictionary<uint, UserAudioPlayer> _players = new ConcurrentDictionary<uint, UserAudioPlayer>();

        private SpeechCodec _transmissionCodec;

        public VoiceService(MumbleConnection connection, UsersManagementService usersManagementService)
        {
            _connection = connection;
            _connection.RegisterPacketProcessor(new PacketProcessor(PacketType.CodecVersion, ProcessCodecVersionPacket));
            _connection.RegisterVoicePacketProcessor(ProcessPackage);

            _usersManagementService = usersManagementService;
            _usersManagementService.UserJoined += UsersManagementServiceOnUserJoined;
            _usersManagementService.UserLeft += UsersManagementServiceOnUserLeft;
        }

        private void UsersManagementServiceOnUserLeft(object sender, UserEventArgs e)
        {
            if (_players.TryRemove(e.User.Id, out var player))
            {
                player.Dispose();
            }
        }

        private void UsersManagementServiceOnUserJoined(object sender, UserEventArgs e)
        {
            _players.AddOrUpdate(e.User.Id, new UserAudioPlayer(e.User.Id), (u, player) => player);
        }

        private void ProcessCodecVersionPacket(object packet)
        {
            var codecVersion = (CodecVersion)packet;
            if (codecVersion.Opus)
                _transmissionCodec = SpeechCodec.Opus;
            else if (codecVersion.PreferAlpha)
                _transmissionCodec = SpeechCodec.CeltAlpha;
            else
                _transmissionCodec = SpeechCodec.CeltBeta;
        }

        private void ProcessPackage(byte[] packet, int type)
        {
            // var vType = (SpeechCodec)type;
            // var target = (SpeechTarget)(packet[0] & 0x1F);

            using (var reader = new UdpPacketReader(new MemoryStream(packet, 1, packet.Length - 1)))
            {
                UInt32 session = (uint)reader.ReadVarInt64();
                Int64 sequence = reader.ReadVarInt64();

                var size = (int)reader.ReadVarInt64();
                size &= 0x1fff;

                if (size == 0)
                    return;

                byte[] data = reader.ReadBytes(size);
                if (data == null)
                    return;

                if(_players.TryGetValue(session, out var player))
                {
                    player.ProcessEncodedVoice(data, sequence);
                }
            }
        }
    }
}
