using System;
using JetBrains.Annotations;

namespace MumbleSharp.Services.UsersManagement
{
    public class User : IEquatable<User>
    {
        [PublicAPI]
        public uint Id { get; }

        [PublicAPI]
        public bool Deaf { get; internal set; }

        [PublicAPI]
        public bool Muted { get; internal set; }

        [PublicAPI]
        public bool SelfDeaf { get; internal set; }

        [PublicAPI]
        public bool SelfMuted { get; internal set; }

        [PublicAPI]
        public bool Suppress { get; internal set; }

        [PublicAPI]
        public Channel Channel { get; internal set; }

        [PublicAPI]
        public string Name { get; internal set; }

        [PublicAPI]
        public string Comment { get; internal set; }

        public User(uint id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(User other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((User) obj);
        }

        public override int GetHashCode()
        {
            return (int) Id;
        }
    }
}