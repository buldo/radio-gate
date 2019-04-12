using MumbleSharp.Packets;
using System;

namespace MumbleSharp.Voice
{
    public static class VoiceExtensions
    {
        public static void UseVoice(this MumbleConnection connection, VoiceSubsystem voiceSubsystem)
        {
            connection.RegisterPacketProcessor(PacketType.UserState, voiceSubsystem.UserStateProcessor);
            connection.RegisterPacketProcessor(PacketType.UserRemove, voiceSubsystem.UserRemoveProcessor);
        }
    }
}
