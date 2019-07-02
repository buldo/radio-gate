
using JetBrains.Annotations;
using MumbleSharp.Services.UsersManagement;

namespace MumbleSharp.Services.TextMessaging
{
    public abstract class BaseMessage
    {
        [PublicAPI]
        public User Sender { get; protected set; }

        [PublicAPI]
        public string Text { get; protected set; }

        protected BaseMessage(User sender, string text)
        {
            Sender = sender;
            Text = text;
        }
    }
}