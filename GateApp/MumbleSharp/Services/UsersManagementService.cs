using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MumbleProto;
using MumbleSharp.Model;
using MumbleSharp.Packets;

namespace MumbleSharp.Services
{
    public class UsersManagementService : IService
    {
        private readonly ServerSyncStateService _serverSyncStateService;
        private readonly ConcurrentDictionary<UInt32, User> _users = new ConcurrentDictionary<UInt32, User>();
        private readonly ConcurrentDictionary<UInt32, Channel> _channels = new ConcurrentDictionary<UInt32, Channel>();
        public UsersManagementService(ServerSyncStateService serverSyncStateService)
        {
            _serverSyncStateService = serverSyncStateService;
            _serverSyncStateService.SyncReceived += ServerSyncStateServiceOnSyncReceived;
        }

        public User LocalUser { get; private set; }

        public IEnumerable<User> GetUsers() => _users.Values;

        public bool TryGetUser(UInt32 sessionId, out User user) => _users.TryGetValue(sessionId, out user);

        public bool TryGetChannel(uint channelId, out Channel channel) => _channels.TryGetValue(channelId, out channel);

        IEnumerable<PacketProcessor> IService.GetProcessors()
        {
            return new[]
            {
                new PacketProcessor(PacketType.UserState, ProcessUserStatePacket),
                new PacketProcessor(PacketType.UserRemove, ProcessUserRemovePacket),
                new PacketProcessor(PacketType.ChannelState, ProcessChannelStatePacket),
                new PacketProcessor(PacketType.ChannelRemove, ProcessChannelRemovePacket),
            };
        }

        public Channel[] GetChannels() => _channels.Values.ToArray();

        private void ProcessUserStatePacket(object packet)
        {
            var userState = (UserState)packet;

            if (userState.ShouldSerializeSession())
            {
                bool added = false;
                User user = _users.AddOrUpdate(userState.Session, i =>
                {
                    added = true;
                    return new User(userState.Session);
                }, (i, u) => u);

                if (userState.ShouldSerializeSelfDeaf())
                    user.SelfDeaf = userState.SelfDeaf;
                if (userState.ShouldSerializeSelfMute())
                    user.SelfMuted = userState.SelfMute;
                if (userState.ShouldSerializeMute())
                    user.Muted = userState.Mute;
                if (userState.ShouldSerializeDeaf())
                    user.Deaf = userState.Deaf;
                if (userState.ShouldSerializeSuppress())
                    user.Suppress = userState.Suppress;
                if (userState.ShouldSerializeName())
                    user.Name = userState.Name;
                if (userState.ShouldSerializeComment())
                    user.Comment = userState.Comment;

                if (userState.ShouldSerializeChannelId())
                    user.Channel = _channels[userState.ChannelId];
                else
                {
                    if (_channels.TryGetValue(0, out var channel))
                    {
                        user.Channel = channel;
                    }
                    else
                    {
                        user.Channel = null;
                    }
                }

            }
        }

        /// <summary>
        /// A user has been removed from the server (left, kicked or banned)
        /// </summary>
        private void ProcessUserRemovePacket(object packet)
        {
            var userRemove = (UserRemove)packet;

            if (_users.TryRemove(userRemove.Session, out var user))
            {
                user.Channel = null;

                //UserLeft(user);
            }

            // TODO: сделать что-то с этим
            //if (user.Equals(LocalUser))
            //    _connection.Close();
        }

        /// <summary>
        /// Server has changed some detail of a channel
        /// </summary>
        private void ProcessChannelStatePacket(object packet)
        {
            var channelState = (ChannelState)packet;
            var channel = _channels.AddOrUpdate(channelState.ChannelId,
                i => new Channel(channelState.ChannelId, channelState.Name, channelState.Parent)
                    { Temporary = channelState.Temporary },
                (i, c) =>
                {
                    c.Name = channelState.Name;
                    return c;
                }
            );
        }

        /// <summary>
        /// Server has removed a channel
        /// </summary>
        private void ProcessChannelRemovePacket(object packet)
        {
            var channelRemove = (ChannelRemove)packet;
            if (_channels.TryRemove(channelRemove.ChannelId, out var c))
            {
                //ChannelLeft(c);
            }
        }

        private void ServerSyncStateServiceOnSyncReceived(object sender, EventArgs e)
        {
            LocalUser = _users[_serverSyncStateService.Session];
        }
    }
}
