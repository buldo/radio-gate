using System;
using System.Linq;
using JetBrains.Annotations;
using MumbleProto;
using MumbleSharp.Model;
using MumbleSharp.Packets;

namespace MumbleSharp.Services
{
    public class TextMessagingService
    {
        private readonly MumbleConnection _connection;
        private readonly UsersManagementService _usersManagementService;

        public event EventHandler<PersonalMessageEventArgs> PersonalMessageReceived;
        public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived;

        public TextMessagingService(MumbleConnection connection, UsersManagementService usersManagementService)
        {
            _connection = connection;
            _usersManagementService = usersManagementService;
            _connection.RegisterPacketProcessor(new PacketProcessor(PacketType.TextMessage, ProcessTextMessagePacket));
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        [PublicAPI]
        public void SendMessage(User user, string message)
        {
            var messages = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            SendMessage(user, messages);
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        [PublicAPI]
        public void SendMessage(User user, string[] message)
        {
            _connection.SendControl(PacketType.TextMessage, new TextMessage
            {
                Actor = _usersManagementService.LocalUser.Id,
                Message = string.Join(Environment.NewLine, message),
            });
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        [PublicAPI]
        public void SendMessage(Channel channel, string[] message, bool recursive)
        {
            var msg = new TextMessage
            {
                Actor = _usersManagementService.LocalUser.Id,
                Message = string.Join(Environment.NewLine, message),
            };

            if (recursive)
            {
                if (msg.TreeIds == null)
                {
                    msg.TreeIds = new [] { channel.Id };
                }
                else
                {
                    msg.TreeIds = msg.TreeIds.Concat(new uint[] { channel.Id }).ToArray();
                }
            }
            else
            {
                if (msg.ChannelIds == null)
                {
                    msg.ChannelIds = new [] { channel.Id };
                }
                else
                {
                    msg.ChannelIds = msg.ChannelIds.Concat(new [] { channel.Id }).ToArray();
                }
            }

            _connection.SendControl(PacketType.TextMessage, msg);
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        [PublicAPI]
        public void SendMessage(Channel channel, string message, bool recursive)
        {
            var messages = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            SendMessage(channel, messages, recursive);
        }

        /// <summary>
        /// Received a text message from the server
        /// </summary>
        private void ProcessTextMessagePacket(object packet)
        {
            var textMessage = (TextMessage)packet;

            if (!_usersManagementService.TryGetUser(textMessage.Actor, out var user)) //If we don't know the user for this packet, just ignore it
                return;

            if (textMessage.ChannelIds == null || textMessage.ChannelIds.Length == 0)
            {
                if (textMessage.TreeIds == null || textMessage.TreeIds.Length == 0)
                {
                    //personal message: no channel, no tree
                    PersonalMessageReceived?.Invoke(
                        this,
                        new PersonalMessageEventArgs(
                            new PersonalMessage(user, string.Join("", textMessage.Message))));
                }
                else
                {
                    //recursive message: sent to multiple channels
                    if (!_usersManagementService.TryGetChannel(textMessage.TreeIds[0], out var channel)
                    ) //If we don't know the channel for this packet, just ignore it
                        return;

                    //TODO: This is a *tree* message - trace down the entire tree (using IDs in textMessage.TreeId as roots) and call ChannelMessageReceived for every channel
                    ChannelMessageReceived?.Invoke(
                        this,
                        new ChannelMessageEventArgs(
                            new ChannelMessage(user, string.Join("", textMessage.Message), channel, true)));
                }
            }
            else
            {
                foreach (uint channelId in textMessage.ChannelIds)
                {
                    if (!_usersManagementService.TryGetChannel(channelId, out var channel))
                        continue;

                    ChannelMessageReceived?.Invoke(
                        this,
                        new ChannelMessageEventArgs(
                            new ChannelMessage(user, string.Join("", textMessage.Message), channel)));
                }
            }
        }

        public class PersonalMessageEventArgs : EventArgs
        {
            internal PersonalMessageEventArgs(PersonalMessage message)
            {
                Message = message;
            }

            public PersonalMessage Message { get; }
        }

        public class ChannelMessageEventArgs : EventArgs
        {
            internal ChannelMessageEventArgs(ChannelMessage message)
            {
                Message = message;
            }

            public ChannelMessage Message { get; }
        }
    }
}
