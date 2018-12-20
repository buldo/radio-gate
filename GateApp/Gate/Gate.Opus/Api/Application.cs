namespace Gate.Opus.Api
{
    /// <summary>
    /// Supported coding modes.
    /// </summary>
    public enum Application
    {
        /// <summary>
        /// Best for most VoIP/videoconference applications where listening quality and intelligibility matter most.
        /// </summary>
        VoIP = 2048,
        /// <summary>
        /// Best for broadcast/high-fidelity application where the decoded audio should be as close as possible to input.
        /// </summary>
        Audio = 2049,
        /// <summary>
        /// Only use when lowest-achievable latency is what matters most. Voice-optimized modes cannot be used.
        /// </summary>
        RestrictedLowLatency = 2051
    }
}
