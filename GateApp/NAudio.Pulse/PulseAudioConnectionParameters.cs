namespace NAudio.Pulse
{
    public class PulseAudioConnectionParameters
    {
        public PulseAudioConnectionParameters(
            string serverName,
            string applicationName,
            string device,
            string streamName)
        {
            ServerName = serverName;
            ApplicationName = applicationName;
            Device = device;
            StreamName = streamName;
        }

        public string ServerName { get; }
        public string ApplicationName { get; }
        public string Device { get; }
        public string StreamName { get; }
    }
}