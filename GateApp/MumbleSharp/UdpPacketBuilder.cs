using System;
using System.Collections.Generic;
using System.Text;

namespace MumbleSharp
{
    internal class UdpPacketBuilder
    {
        private readonly List<byte> _buffer = new List<byte>();

        public void WriteByte(byte value)
        {
            _buffer.Add(value);
        }

        public void WriteVarLong(long value)
        {
            ulong i = (ulong)value;

            if (((i & 0x8000000000000000L) > 0) && (~i < 0x100000000L))
            {
                // Signed number.
                i = ~i;
                if (i <= 0x3)
                {
                    // Shortcase for -1 to -4
                    WriteByte((byte) (0xFC | i));
                    return;
                }
                else
                {
                    WriteByte(0xF8);
                }
            }

            if (i < 0x80)
            {
                // Need top bit clear
                WriteByte((byte) i);
            }
            else if (i < 0x4000)
            {
                // Need top two bits clear
                WriteByte((byte) ((i >> 8) | 0x80));
                WriteByte((byte) (i & 0xFF));
            }
            else if (i < 0x200000)
            {
                // Need top three bits clear
                WriteByte((byte) ((i >> 16) | 0xC0));
                WriteByte((byte) ((i >> 8) & 0xFF));
                WriteByte((byte) (i & 0xFF));
            }
            else if (i < 0x10000000)
            {
                // Need top four bits clear
                WriteByte((byte) ((i >> 24) | 0xE0));
                WriteByte((byte) ((i >> 16) & 0xFF));
                WriteByte((byte) ((i >> 8) & 0xFF));
                WriteByte((byte) (i & 0xFF));
            }
            else if (i < 0x100000000L)
            {
                // It's a full 32-bit integer.
                WriteByte(0xF0);
                WriteByte((byte) ((i >> 24) & 0xFF));
                WriteByte((byte) ((i >> 16) & 0xFF));
                WriteByte((byte) ((i >> 8) & 0xFF));
                WriteByte((byte) (i & 0xFF));
            }
            else
            {
                // It's a 64-bit value.
                WriteByte(0xF4);
                WriteByte((byte) ((i >> 56) & 0xFF));
                WriteByte((byte) ((i >> 48) & 0xFF));
                WriteByte((byte) ((i >> 40) & 0xFF));
                WriteByte((byte) ((i >> 32) & 0xFF));
                WriteByte((byte) ((i >> 24) & 0xFF));
                WriteByte((byte) ((i >> 16) & 0xFF));
                WriteByte((byte) ((i >> 8) & 0xFF));
                WriteByte((byte) (i & 0xFF));
            }
        }

        public void Write(in IEnumerable<byte> packet)
        {
            _buffer.AddRange(packet);
        }

        public byte[] ToArray() => _buffer.ToArray();
    }
}
