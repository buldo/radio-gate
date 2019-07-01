using System;

namespace MumbleSharp.Model
{
    public class User : IEquatable<User>
    {
        public uint Id { get; }
        public bool Deaf { get; set; }
        public bool Muted { get; set; }
        public bool SelfDeaf { get; set; }
        public bool SelfMuted { get; set; }
        public bool Suppress { get; set; }

        public Channel Channel { get; set; }

        public string Name { get; set; }
        public string Comment { get; set; }

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