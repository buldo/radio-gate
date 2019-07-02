using JetBrains.Annotations;
using MumbleSharp.Services.UsersManagement;

namespace MumbleSharp.Services.TextMessaging
{
    public class ChannelMessage : BaseMessage
    {
        [PublicAPI]
        public Channel Channel { get; }

        [PublicAPI]
        public bool IsRecursive { get; }

        public ChannelMessage(User sender, string text, Channel channel, bool isRecursive = false)
            :base(sender, text)
        {
            Channel = channel;
            IsRecursive = isRecursive;
        }
    }
}