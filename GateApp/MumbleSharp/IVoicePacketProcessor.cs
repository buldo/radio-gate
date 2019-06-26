namespace MumbleSharp
{
    public interface IVoicePacketProcessor
    {
        void ProcessPackage(byte[] data, int type);
    }
}