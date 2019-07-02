using System;

namespace MumbleSharp.Services.UsersManagement
{
    public class UserEventArgs : EventArgs
    {
        public UserEventArgs(User user)
        {
            User = user;
        }

        public User User { get; }
    }
}
