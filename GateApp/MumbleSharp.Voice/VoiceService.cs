using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MumbleProto;
using MumbleSharp.Packets;
using MumbleSharp.Services;
using MumbleSharp.Services.UsersManagement;
using MumbleSharp.Voice.Codecs;

namespace MumbleSharp.Voice
{
    public class VoiceService
    {
        private readonly ILogger _logger;
        private readonly MumbleConnection _connection;
        private readonly UsersManagementService _usersManagementService;
        private readonly ConcurrentDictionary<uint, UserAudioPlayer> _players = new ConcurrentDictionary<uint, UserAudioPlayer>();
        private readonly BufferedEncoder _bufferedEncoder;

        public VoiceService(
            MumbleConnection connection,
            UsersManagementService usersManagementService,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<VoiceService>();

            _bufferedEncoder = new BufferedEncoder(loggerFactory);

            _connection = connection;
            _connection.RegisterPacketProcessor(new PacketProcessor(PacketType.CodecVersion, ProcessCodecVersionPacket));
            _connection.RegisterVoicePacketProcessor(ProcessIncomingVoicePackage);

            _usersManagementService = usersManagementService;
            _usersManagementService.UserJoined += UsersManagementServiceOnUserJoined;
            _usersManagementService.UserLeft += UsersManagementServiceOnUserLeft;

            var sendingTask = Task.Factory.StartNew(() => SendEncodedFramesAsync(_bufferedEncoder.EncodedFrames), TaskCreationOptions.LongRunning);
        }

        public bool IsSendingToServer { get; private set; }

        public event EventHandler<EventArgs> UsersStateChanged;

        public void StartSendingVoice()
        {
            if(!IsSendingToServer)
            {
                IsSendingToServer = true;

                // TODO: Добавить всякого разного, запускающего отправку голоса
            }
        }

        public void StopSendingVoice()
        {
            if(IsSendingToServer)
            {
                IsSendingToServer = false;
                // TODO: добавить отправку стоп бита
            }
        }

        public IEnumerable<(uint UserId, UserVoiceState State)> GetUsersState()
        {
            return _players.Select(p => (p.Key, p.Value.State)).ToArray();
        }

        public void AddVoiceFramesToSendBuffer(Span<byte> pcm)
        {
            if (IsSendingToServer)
            {
                _bufferedEncoder.AddPcm(pcm);
            }
        }

        private void UsersManagementServiceOnUserLeft(object sender, UserEventArgs e)
        {
            if (_players.TryRemove(e.User.Id, out var player))
            {
                player.StateChanged -= PlayerOnStateChanged;
                player.Dispose();
            }
        }

        private void UsersManagementServiceOnUserJoined(object sender, UserEventArgs e)
        {
            if(!_players.ContainsKey(e.User.Id))
            {
                var player = new UserAudioPlayer(e.User.Id);
                player.StateChanged += PlayerOnStateChanged;
                if(_players.TryAdd(e.User.Id, player))
                {
                    player.StateChanged -= PlayerOnStateChanged;
                    player.Dispose();
                }
            }
        }

        private void PlayerOnStateChanged(object sender, VoiceStateChangedEventArgs e)
        {
            UsersStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ProcessCodecVersionPacket(object packet)
        {
            var codecVersion = (CodecVersion)packet;
            if (!codecVersion.Opus)
            {
                throw new NotImplementedException("Only OPUS supported");
            }
        }

        private void ProcessIncomingVoicePackage(byte[] packet, int type)
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
                else
                {
                    _logger.LogWarning($"Voice package for unknown session {session}");
                }
            }
        }

        private async Task SendEncodedFramesAsync(ChannelReader<byte[]> reader)
        {
            try
            {
                while (true)
                {
                    var data = await reader.ReadAsync();
                    _connection.SendEncodedVoice(data);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "EncodedFramesSender exception");
                throw;
            }

        }
    }
}
