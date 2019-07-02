using System;
using JetBrains.Annotations;

namespace MumbleSharp.Services.UsersManagement
{
    public class Channel : IEquatable<Channel>
    {
        [PublicAPI]
        public uint Id { get; }

        [PublicAPI]
        public bool Temporary { get; internal set; }

        [PublicAPI]
        public string Name { get; internal set; }

        [PublicAPI]
        public uint Parent { get; }

        internal Channel(uint id, string name, uint parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(Channel other)
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
            return Equals((Channel) obj);
        }

        public override int GetHashCode()
        {
            return (int) Id;
        }

        public static bool operator ==(Channel left, Channel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Channel left, Channel right)
        {
            return !Equals(left, right);
        }
    }
}
