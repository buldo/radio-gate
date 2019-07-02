using MumbleSharp.Services.UsersManagement;

namespace MumbleSharp.Services.TextMessaging
{
    public class PersonalMessage : BaseMessage
    {
        public PersonalMessage(User sender, string text)
            :base(sender, text)
        {
        }
    }
}