using System.Threading;
using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MumbleSharp.Packets;
using Version = MumbleProto.Version;

namespace MumbleSharp
{
    /// <summary>
    /// A basic mumble protocol which handles events from the server - override the individual handler methods to replace/extend the default behaviour
    /// </summary>
    public class BasicMumbleProtocol
    {
        private MumbleConnection _connection;

        protected readonly ConcurrentDictionary<UInt32, User> UserDictionary = new ConcurrentDictionary<UInt32, User>();

        public IEnumerable<User> Users
        {
            get { return UserDictionary.Values; }
        }

        protected readonly ConcurrentDictionary<UInt32, Channel> ChannelDictionary =
            new ConcurrentDictionary<UInt32, Channel>();

        public IEnumerable<Channel> Channels
        {
            get { return ChannelDictionary.Values; }
        }

        public Channel RootChannel { get; private set; }

        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        public bool ReceivedServerSync { get; private set; }

        public SpeechCodec TransmissionCodec { get; private set; }

        public User LocalUser { get; private set; }

        private AudioEncodingBuffer _encodingBuffer;
        private Thread _encodingThread;
        private UInt32 sequenceIndex;

        public bool IsEncodingThreadRunning { get; set; }

        public BasicMumbleProtocol(MumbleConnection connection)
        {
            _connection = connection;
            _connection.PacketReceived += ConnectionOnPacketReceived;
            _connection.EncodedVoiceReceived += ConnectionOnEncodedVoiceReceived;
            _encodingThread = new Thread(EncodingThreadEntry)
            {
                IsBackground = true
            };
        }

        public void SendMessage(IEnumerable<Channel> channels, string[] message, bool recursive)
        {
            // It's conceivable that this group could include channels from multiple different server connections
            // group by server
            foreach (var group in channels.GroupBy(a => a.Owner))
            {
                var owner = group.First().Owner;

                var msg = new TextMessage
                {
                    Actor = owner.LocalUser.Id,
                    Message = string.Join(Environment.NewLine, message),
                };

                if (recursive)
                {
                    if (msg.TreeIds == null)
                        msg.TreeIds = group.Select(c => c.Id).ToArray();
                    else
                        msg.TreeIds = msg.TreeIds.Concat(group.Select(c => c.Id)).ToArray();
                }
                else
                {
                    if (msg.ChannelIds == null)
                        msg.ChannelIds = group.Select(c => c.Id).ToArray();
                    else
                        msg.ChannelIds = msg.ChannelIds.Concat(group.Select(c => c.Id)).ToArray();
                }

                _connection.SendControl<TextMessage>(PacketType.TextMessage, msg);
            }
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">A text message (which will be split on newline characters)</param>
        public void SendMessage(User user, string message)
        {
            var messages = message.Split( new []{"\r\n", "\n"}, StringSplitOptions.None);
            SendMessage(user, messages);
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Individual lines of a text message</param>
        public void SendMessage(User user, string[] message)
        {
            _connection.SendControl<TextMessage>(PacketType.TextMessage, new TextMessage
            {
                Actor = LocalUser.Id,
                Message = string.Join(Environment.NewLine, message),
            });
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Individual lines of a text message</param>
        public void SendMessage(Channel channel, string[] message, bool recursive)
        {
            var msg = new TextMessage
            {
                Actor = LocalUser.Id,
                Message = string.Join(Environment.NewLine, message),
            };

            if (recursive)
            {
                if (msg.TreeIds == null)
                    msg.TreeIds = new uint[] { channel.Id };
                else
                    msg.TreeIds = msg.TreeIds.Concat(new uint[] { channel.Id }).ToArray();
            }
            else
            {
                if (msg.ChannelIds == null)
                    msg.ChannelIds = new uint[] { channel.Id };
                else
                    msg.ChannelIds = msg.ChannelIds.Concat(new uint[] { channel.Id }).ToArray();
            }

            _connection.SendControl<TextMessage>(PacketType.TextMessage, msg);
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">A text message (which will be split on newline characters)</param>
        public void SendMessage(Channel channel, string message, bool recursive)
        {
            var messages = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            SendMessage(channel, messages, recursive);
        }

        /// <summary>
        /// Move user to a channel
        /// </summary>
        public void MoveUser(User user, Channel channel)
        {
            if (user.Channel == channel)
                return;

            var userState = new UserState { Actor = user.Id, ChannelId = channel.Id };

            _connection.SendControl<UserState>(PacketType.UserState, userState);
        }

        public void JoinChannel(Channel channel)
        {
            var state = new UserState
            {
                Session = LocalUser.Id,
                Actor = LocalUser.Id,
                ChannelId = channel.Id
            };

            _connection.SendControl<UserState>(PacketType.UserState, state);
        }

        public void Close()
        {
            _encodingThread.Abort();

            _connection = null;
            LocalUser = null;
        }

        /// <summary>
        /// Server has sent a version update
        /// </summary>
        /// <param name="version"></param>
        public virtual void Version(Version version)
        {

        }

        public virtual void SuggestConfig(SuggestConfig config)
        {

        }

        #region Channels

        protected virtual void ChannelJoined(Channel channel)
        {
        }

        protected virtual void ChannelLeft(Channel channel)
        {
        }

        /// <summary>
        /// Server has changed some detail of a channel
        /// </summary>
        /// <param name="channelState"></param>
        public virtual void ChannelState(ChannelState channelState)
        {
            var channel = ChannelDictionary.AddOrUpdate(channelState.ChannelId,
                i => new Channel(this, channelState.ChannelId, channelState.Name, channelState.Parent)
                    {Temporary = channelState.Temporary},
                (i, c) =>
                {
                    c.Name = channelState.Name;
                    return c;
                }
            );

            if (channel.Id == 0)
                RootChannel = channel;

            ChannelJoined(channel);

            Extensions.Log.Info("Chanel State", channelState);
        }

        /// <summary>
        /// Server has removed a channel
        /// </summary>
        /// <param name="channelRemove"></param>
        public virtual void ChannelRemove(ChannelRemove channelRemove)
        {
            Channel c;
            if (ChannelDictionary.TryRemove(channelRemove.ChannelId, out c))
            {
                ChannelLeft(c);
            }
        }

        #endregion

        #region users

        protected virtual void UserJoined(User user)
        {
        }

        protected virtual void UserLeft(User user)
        {
        }

        /// <summary>
        /// Server has changed some detail of a user
        /// </summary>
        /// <param name="userState"></param>
        public virtual void UserState(UserState userState)
        {
            Extensions.Log.Info("User State", userState);

            if (userState.ShouldSerializeSession())
            {
                bool added = false;
                User user = UserDictionary.AddOrUpdate(userState.Session, i =>
                {
                    added = true;
                    return new User(this, userState.Session);
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
                    user.Channel = ChannelDictionary[userState.ChannelId];
                else
                    user.Channel = RootChannel;

                //if (added)
                UserJoined(user);
            }
        }

        /// <summary>
        /// A user has been removed from the server (left, kicked or banned)
        /// </summary>
        /// <param name="userRemove"></param>
        public virtual void UserRemove(UserRemove userRemove)
        {
            User user;
            if (UserDictionary.TryRemove(userRemove.Session, out user))
            {
                user.Channel = null;

                UserLeft(user);
            }

            if (user.Equals(LocalUser))
                _connection.Close();
        }

        #endregion

        public virtual void ContextAction(ContextAction contextAction)
        {
        }

        public virtual void ContextActionModify(ContextActionModify contextActionModify)
        {
        }

        public virtual void PermissionQuery(PermissionQuery permissionQuery)
        {

        }

        #region server setup

        /// <summary>
        /// Initial connection to the server
        /// </summary>
        /// <param name="serverSync"></param>
        public virtual void ServerSync(ServerSync serverSync)
        {
            if (LocalUser != null)
                throw new InvalidOperationException("Second ServerSync Received");

            //Get the local user
            LocalUser = UserDictionary[serverSync.Session];

            _encodingBuffer = new AudioEncodingBuffer();
            _encodingThread.Start();

            ReceivedServerSync = true;
        }

        /// <summary>
        /// Some detail of the server configuration has changed
        /// </summary>
        /// <param name="serverConfig"></param>
        public virtual void ServerConfig(ServerConfig serverConfig)
        {

        }

        #endregion

        #region voice

        private void EncodingThreadEntry()
        {
            IsEncodingThreadRunning = true;
            while (IsEncodingThreadRunning)
            {
                byte[] packet = null;
                try
                {
                    packet = _encodingBuffer.Encode(TransmissionCodec);
                }
                catch
                {
                }

                if (packet != null)
                {
                    int maxSize = 480;

                    //taken from JS port
                    for (int currentOffcet = 0; currentOffcet < packet.Length;)
                    {
                        int currentBlockSize = Math.Min(packet.Length - currentOffcet, maxSize);

                        byte type = TransmissionCodec == SpeechCodec.Opus ? (byte) 4 : (byte) 0;
                        //originaly [type = codec_type_id << 5 | whistep_chanel_id]. now we can talk only to normal chanel
                        type = (byte) (type << 5);
                        byte[] sequence = Var64.writeVarint64_alternative((UInt64) sequenceIndex);

                        // Client side voice header.
                        byte[] voiceHeader = new byte[1 + sequence.Length];
                        voiceHeader[0] = type;
                        sequence.CopyTo(voiceHeader, 1);

                        byte[] header = Var64.writeVarint64_alternative((UInt64) currentBlockSize);
                        byte[] packedData = new byte[voiceHeader.Length + header.Length + currentBlockSize];

                        Array.Copy(voiceHeader, 0, packedData, 0, voiceHeader.Length);
                        Array.Copy(header, 0, packedData, voiceHeader.Length, header.Length);
                        Array.Copy(packet, currentOffcet, packedData, voiceHeader.Length + header.Length,
                            currentBlockSize);

                        _connection.SendVoice(new ArraySegment<byte>(packedData));

                        sequenceIndex++;
                        currentOffcet += currentBlockSize;
                    }
                }

                //beware! can take a lot of power, because infinite loop without sleep
            }
        }

        public virtual void CodecVersion(CodecVersion codecVersion)
        {
            if (codecVersion.Opus)
                TransmissionCodec = SpeechCodec.Opus;
            else if (codecVersion.PreferAlpha)
                TransmissionCodec = SpeechCodec.CeltAlpha;
            else
                TransmissionCodec = SpeechCodec.CeltBeta;
        }

        /// <summary>
        /// Get a voice decoder for the specified user/codec combination
        /// </summary>
        /// <param name="session"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public virtual IVoiceCodec GetCodec(uint session, SpeechCodec codec)
        {
            User user;
            if (!UserDictionary.TryGetValue(session, out user))
                return null;

            return user.GetCodec(codec);
        }

        /// <summary>
        /// Received a UDP ping from the server
        /// </summary>
        /// <param name="packet"></param>
        public virtual void UdpPing(byte[] packet)
        {
        }

        /// <summary>
        /// Received a voice packet from the server
        /// </summary>
        protected virtual void EncodedVoice(
            byte[] data,
            uint sessionId,
            long sequence,
            SpeechCodec codec,
            SpeechTarget target)
        {
        }

        public void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        {
            _encodingBuffer.Add(pcm, target, targetId);
        }

        public void SendVoiceStop()
        {
            _encodingBuffer.Stop();
            sequenceIndex = 0;
        }

        #endregion

        /// <summary>
        /// Received a ping over the TCP connection
        /// </summary>
        /// <param name="ping"></param>
        public virtual void Ping(Ping ping)
        {

        }

        #region text messages

        /// <summary>
        /// Received a text message from the server
        /// </summary>
        /// <param name="textMessage"></param>
        public virtual void TextMessage(TextMessage textMessage)
        {
            User user;
            if (!UserDictionary.TryGetValue(textMessage.Actor, out user)
            ) //If we don't know the user for this packet, just ignore it
                return;

            if (textMessage.ChannelIds == null || textMessage.ChannelIds.Length == 0)
            {
                if (textMessage.TreeIds == null || textMessage.TreeIds.Length == 0)
                {
                    //personal message: no channel, no tree
                    PersonalMessageReceived(new PersonalMessage(user, string.Join("", textMessage.Message)));
                }
                else
                {
                    //recursive message: sent to multiple channels
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(textMessage.TreeIds[0], out channel)
                    ) //If we don't know the channel for this packet, just ignore it
                        return;

                    //TODO: This is a *tree* message - trace down the entire tree (using IDs in textMessage.TreeId as roots) and call ChannelMessageReceived for every channel
                    ChannelMessageReceived(
                        new ChannelMessage(user, string.Join("", textMessage.Message), channel, true));
                }
            }
            else
            {
                foreach (uint channelId in textMessage.ChannelIds)
                {
                    Channel channel;
                    if (!ChannelDictionary.TryGetValue(channelId, out channel))
                        continue;

                    ChannelMessageReceived(new ChannelMessage(user, string.Join("", textMessage.Message), channel));
                }

            }
        }

        protected virtual void PersonalMessageReceived(PersonalMessage message)
        {
        }

        protected virtual void ChannelMessageReceived(ChannelMessage message)
        {
        }

        #endregion

        public virtual void UserList(UserList userList)
        {
        }

        private void ConnectionOnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            switch (e.PacketType)
            {
                case PacketType.Version:
                    Version((Version) e.Packet);
                    break;
                case PacketType.ChannelState:
                    ChannelState((ChannelState) e.Packet);
                    break;
                case PacketType.UserState:
                    UserState((UserState) e.Packet);
                    break;
                case PacketType.CodecVersion:
                    CodecVersion((CodecVersion) e.Packet);
                    break;
                case PacketType.ContextAction:
                    ContextAction((ContextAction) e.Packet);
                    break;
                case PacketType.ContextActionModify:
                    ContextActionModify((ContextActionModify) e.Packet);
                    break;
                case PacketType.PermissionQuery:
                    PermissionQuery((PermissionQuery) e.Packet);
                    break;
                case PacketType.ServerSync:
                    ServerSync((ServerSync) e.Packet);
                    break;
                case PacketType.ServerConfig:
                    ServerConfig((ServerConfig) e.Packet);
                    break;
                case PacketType.UserRemove:
                    UserRemove((UserRemove) e.Packet);
                    break;
                case PacketType.ChannelRemove:
                    ChannelRemove((ChannelRemove) e.Packet);
                    break;
                case PacketType.TextMessage:
                    TextMessage((TextMessage) e.Packet);
                    break;
                case PacketType.UserList:
                    UserList((UserList) e.Packet);
                    break;

                case PacketType.SuggestConfig:
                    SuggestConfig((SuggestConfig) e.Packet);
                    break;
            }
        }

        private void ConnectionOnEncodedVoiceReceived(object sender, EncodedVoiceReceivedEventArgs e)
        {
            EncodedVoice(e.Data, e.Session, e.Sequence, e.Codec, e.Target);
        }
    }
}
