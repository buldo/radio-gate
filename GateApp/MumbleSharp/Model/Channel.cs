using System;

namespace MumbleSharp.Model
{
    public class Channel : IEquatable<Channel>
    {
        public bool Temporary { get; set; }
        public string Name { get; set; }
        public uint Id { get; private set; }
        public uint Parent { get; private set; }

        public Channel(uint id, string name, uint parent)
        {
            Id = id;
            Name = name;
            Parent = parent;
        }

        private static readonly string[] _split = { "\r\n", "\n" };

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
            return Equals((Channel)obj);
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
