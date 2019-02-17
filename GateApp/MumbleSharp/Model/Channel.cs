﻿
using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MumbleSharp.Model
{
    public class Channel
        : IEquatable<Channel>
    {
        internal readonly BasicMumbleProtocol Owner;

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

        public Channel(BasicMumbleProtocol owner, uint id, string name, uint parent)
        {
            Owner = owner;
            Id = id;
            Name = name;
            Parent = parent;
        }

        private static readonly string[] _split = { "\r\n", "\n" };

        public void SendVoice(ArraySegment<byte> buffer, bool whisper = false)
        {
            Owner.SendVoice(
                buffer,
                target: whisper ? SpeechTarget.WhisperToChannel : SpeechTarget.Normal,
                targetId: Id
            );
        }

        public void SendVoiceStop()
        {
            Owner.SendVoiceStop();
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

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var c = obj as Channel;
            if (c != null)
                return Equals(c);

            return ReferenceEquals(this, obj);
        }

        public bool Equals(Channel other)
        {
            return other.Id == Id;
        }
    }
}
