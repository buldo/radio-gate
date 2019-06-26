
using MumbleProto;
using MumbleSharp.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MumbleSharp.Model
{
    public class Channel : IEquatable<Channel>
    {
        public bool Temporary { get; set; }
        public string Name { get; set; }
        public uint Id { get; private set; }
        public uint Parent { get; private set; }

        // Using a concurrent dictionary as a concurrent hashset (why doesn't .net provide a concurrent hashset?!) - http://stackoverflow.com/a/18923091/108234
        private readonly ConcurrentDictionary<User, bool> _users = new ConcurrentDictionary<User, bool>();
        public IEnumerable<User> Users
        {
            get { return _users.Keys; }
        }

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

        internal void AddUser(User user)
        {
            _users.GetOrAdd(user, true);
        }

        internal void RemoveUser(User user)
        {
            bool _;
            _users.TryRemove(user, out _);
        }
    }
}
