using System;
using MumbleSharp.Model;

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
