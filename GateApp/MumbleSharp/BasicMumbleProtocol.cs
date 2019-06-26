using System.Threading;
using MumbleProto;
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

        private Thread _encodingThread;
        private UInt32 sequenceIndex;

        public bool IsEncodingThreadRunning { get; set; }

        public BasicMumbleProtocol(MumbleConnection connection)
        {
            _connection = connection;

            //_encodingThread = new Thread(EncodingThreadEntry)
            //{
            //    IsBackground = true
            //};
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">A text message (which will be split on newline characters)</param>
        //public void SendMessage(User user, string message)
        //{
        //    var messages = message.Split( new []{"\r\n", "\n"}, StringSplitOptions.None);
        //    SendMessage(user, messages);
        //}

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Individual lines of a text message</param>
        //public void SendMessage(User user, string[] message)
        //{
        //    _connection.SendControl<TextMessage>(PacketType.TextMessage, new TextMessage
        //    {
        //        Actor = LocalUser.Id,
        //        Message = string.Join(Environment.NewLine, message),
        //    });
        //}

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Individual lines of a text message</param>
        //public void SendMessage(Channel channel, string[] message, bool recursive)
        //{
        //    var msg = new TextMessage
        //    {
        //        Actor = LocalUser.Id,
        //        Message = string.Join(Environment.NewLine, message),
        //    };

        //    if (recursive)
        //    {
        //        if (msg.TreeIds == null)
        //            msg.TreeIds = new uint[] { channel.Id };
        //        else
        //            msg.TreeIds = msg.TreeIds.Concat(new uint[] { channel.Id }).ToArray();
        //    }
        //    else
        //    {
        //        if (msg.ChannelIds == null)
        //            msg.ChannelIds = new uint[] { channel.Id };
        //        else
        //            msg.ChannelIds = msg.ChannelIds.Concat(new uint[] { channel.Id }).ToArray();
        //    }

        //    _connection.SendControl<TextMessage>(PacketType.TextMessage, msg);
        //}

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">A text message (which will be split on newline characters)</param>
        //public void SendMessage(Channel channel, string message, bool recursive)
        //{
        //    var messages = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        //    SendMessage(channel, messages, recursive);
        //}

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

        //public void SendVoice(Channel channel, ArraySegment<byte> buffer, bool whisper = false)
        //{
        //    SendVoice(
        //        buffer,
        //        target: whisper ? SpeechTarget.WhisperToChannel : SpeechTarget.Normal,
        //        targetId: channel.Id
        //    );
        //}

        //public void JoinChannel(Channel channel)
        //{
        //    var state = new UserState
        //    {
        //        Session = LocalUser.Id,
        //        Actor = LocalUser.Id,
        //        ChannelId = channel.Id
        //    };

        //    _connection.SendControl<UserState>(PacketType.UserState, state);
        //}

        //public void Close()
        //{
        //    _encodingThread.Abort();

        //    _connection = null;
        //    LocalUser = null;
        //}

        #region Channels

        protected virtual void ChannelJoined(Channel channel)
        {
        }

        protected virtual void ChannelLeft(Channel channel)
        {
        }

        #endregion

        #region server setup

        #endregion

        #region voice

        //private void EncodingThreadEntry()
        //{
        //    IsEncodingThreadRunning = true;
        //    while (IsEncodingThreadRunning)
        //    {
        //        byte[] packet = null;
        //        try
        //        {
        //            packet = _encodingBuffer.Encode(TransmissionCodec);
        //        }
        //        catch
        //        {
        //        }

        //        if (packet != null)
        //        {
        //            int maxSize = 480;

        //            //taken from JS port
        //            for (int currentOffcet = 0; currentOffcet < packet.Length;)
        //            {
        //                int currentBlockSize = Math.Min(packet.Length - currentOffcet, maxSize);

        //                byte type = TransmissionCodec == SpeechCodec.Opus ? (byte) 4 : (byte) 0;
        //                //originaly [type = codec_type_id << 5 | whistep_chanel_id]. now we can talk only to normal chanel
        //                type = (byte) (type << 5);
        //                byte[] sequence = Var64.writeVarint64_alternative((UInt64) sequenceIndex);

        //                // Client side voice header.
        //                byte[] voiceHeader = new byte[1 + sequence.Length];
        //                voiceHeader[0] = type;
        //                sequence.CopyTo(voiceHeader, 1);

        //                byte[] header = Var64.writeVarint64_alternative((UInt64) currentBlockSize);
        //                byte[] packedData = new byte[voiceHeader.Length + header.Length + currentBlockSize];

        //                Array.Copy(voiceHeader, 0, packedData, 0, voiceHeader.Length);
        //                Array.Copy(header, 0, packedData, voiceHeader.Length, header.Length);
        //                Array.Copy(packet, currentOffcet, packedData, voiceHeader.Length + header.Length,
        //                    currentBlockSize);

        //                _connection.SendVoice(new ArraySegment<byte>(packedData));

        //                sequenceIndex++;
        //                currentOffcet += currentBlockSize;
        //            }
        //        }

        //        //beware! can take a lot of power, because infinite loop without sleep
        //    }
        //}

        //public void SendVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        //{
        //    _encodingBuffer.Add(pcm, target, targetId);
        //}

        //public void SendVoiceStop()
        //{
        //    _encodingBuffer.Stop();
        //    sequenceIndex = 0;
        //}

        #endregion
    }
}
