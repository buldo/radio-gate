using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MumbleProto;
using MumbleSharp;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Model;
using MumbleSharp.Packets;
using NAudio.Wave;

namespace MumbleSharp.Demo
{
    /// <summary>
    /// A test mumble protocol. Currently just prints the name of whoever is speaking, as well as printing messages it receives
    /// </summary>
    public class ConsoleMumbleProtocol : BasicMumbleProtocol
    {
        private readonly ConcurrentDictionary<uint, UserAudioPlayer> _audioBuffers = new ConcurrentDictionary<uint, UserAudioPlayer>();


        public ConsoleMumbleProtocol(MumbleConnection connection) : base(connection)
        {
        }

        protected override void EncodedVoice(
            byte[] data,
            uint sessionId,
            long sequence,
            SpeechCodec codec,
            SpeechTarget target)
        {
            if (UserDictionary.TryGetValue(sessionId, out var user))
            {
                Console.WriteLine(user.Name + " is speaking. Seq" + sequence);
            }

            base.EncodedVoice(data, sessionId, sequence, codec, target);

            if (_audioBuffers.TryGetValue(sessionId, out var audioPlayer))
            {
                audioPlayer.ProcessEncodedVoice(data, sequence);
            }
        }

        protected override void UserJoined(User user)
        {
            base.UserJoined(user);

            _audioBuffers.AddOrUpdate(user.Id, new UserAudioPlayer(user.Id), (u, player) => player);
        }

        protected override void UserLeft(User user)
        {
            base.UserLeft(user);

            _audioBuffers.Remove(user.Id, out _);
        }

        public override void ServerConfig(ServerConfig serverConfig)
        {
            base.ServerConfig(serverConfig);

            Console.WriteLine(serverConfig.WelcomeText);
        }

        protected override void ChannelMessageReceived(ChannelMessage message)
        {
            if (message.Channel.Equals(LocalUser.Channel))
                Console.WriteLine(string.Format("{0} (channel message): {1}", message.Sender.Name, message.Text));

            base.ChannelMessageReceived(message);
        }

        protected override void PersonalMessageReceived(PersonalMessage message)
        {
            Console.WriteLine(string.Format("{0} (personal message): {1}", message.Sender.Name, message.Text));

            base.PersonalMessageReceived(message);
        }
    }
}
