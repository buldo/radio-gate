using System;
using System.Collections.Generic;
using System.Text;
using MumbleProto;

namespace MumbleSharp
{
    internal class PingProcessor
    {
        //using the approch described here to do running calculations of ping values.
        // http://dsp.stackexchange.com/questions/811/determining-the-mean-and-standard-deviation-in-real-time
        private float _meanOfPings;
        private float _varianceTimesCountOfPings;
        private int _countOfPings;
        private bool _shouldSetTimestampWhenPinging;

        public float? TcpPingAverage { get; set; }
        public float? TcpPingVariance { get; set; }
        public uint? TcpPingPackets { get; set; }

        public void ReceivePing(Ping ping)
        {
            _shouldSetTimestampWhenPinging = true;
            if (ping.ShouldSerializeTimestamp() && ping.Timestamp != 0)
            {
                var mostRecentPingTime =
                    (float)TimeSpan.FromTicks(DateTime.Now.Ticks - (long)ping.Timestamp).TotalMilliseconds;

                //The ping time is the one-way transit time.
                mostRecentPingTime /= 2;

                var previousMean = _meanOfPings;
                _countOfPings++;
                _meanOfPings = _meanOfPings + ((mostRecentPingTime - _meanOfPings) / _countOfPings);
                _varianceTimesCountOfPings = _varianceTimesCountOfPings +
                                             ((mostRecentPingTime - _meanOfPings) * (mostRecentPingTime - previousMean));

                TcpPingPackets = (uint)_countOfPings;
                TcpPingAverage = _meanOfPings;
                TcpPingVariance = _varianceTimesCountOfPings / _countOfPings;
            }
        }

        public Ping CreateTcpPing()
        {
            var ping = new Ping();

            //Only set the timestamp if we're currently connected.  This prevents the ping stats from being built.
            //  otherwise the stats will be throw off by the time it takes to connect.
            if (_shouldSetTimestampWhenPinging)
            {
                ping.Timestamp = (ulong)DateTime.Now.Ticks;
            }

            if (TcpPingAverage.HasValue)
            {
                ping.TcpPingAvg = TcpPingAverage.Value;
            }
            if (TcpPingVariance.HasValue)
            {
                ping.TcpPingVar = TcpPingVariance.Value;
            }
            if (TcpPingPackets.HasValue)
            {
                ping.TcpPackets = TcpPingPackets.Value;
            }

            return ping;
        }

        public byte[] CreateUdpPing()
        {
            long timestamp = DateTime.Now.Ticks;

            byte[] buffer = new byte[9];
            buffer[0] = 1 << 5;
            buffer[1] = (byte)((timestamp >> 56) & 0xFF);
            buffer[2] = (byte)((timestamp >> 48) & 0xFF);
            buffer[3] = (byte)((timestamp >> 40) & 0xFF);
            buffer[4] = (byte)((timestamp >> 32) & 0xFF);
            buffer[5] = (byte)((timestamp >> 24) & 0xFF);
            buffer[6] = (byte)((timestamp >> 16) & 0xFF);
            buffer[7] = (byte)((timestamp >> 8) & 0xFF);
            buffer[8] = (byte)((timestamp) & 0xFF);

            return buffer;
        }
    }
}
