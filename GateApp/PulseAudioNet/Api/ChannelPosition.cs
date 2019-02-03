namespace PulseAudioNet.Api
{
    public enum ChannelPosition
    {
        Invalid = -1,
        Mono = 0,

        /// <summary>
        /// Apple, Dolby call this 'Left'
        /// </summary>
        FrontLeft,

        /// <summary>
        /// Apple, Dolby call this 'Right'
        /// </summary>
        FrontRight,

        /// <summary>
        /// Apple, Dolby call this 'Center'
        /// </summary>
        FrontCenter,

        Left = FrontLeft,
        Right = FrontRight,
        Center = FrontCenter,

        /// <summary>
        /// Microsoft calls this 'Back Center', Apple calls this 'Center Surround', Dolby calls this 'Surround Rear Center'
        /// </summary>
        RearCenter,

        /// <summary>
        /// Microsoft calls this 'Back Left', Apple calls this 'Left Surround' (!), Dolby calls this 'Surround Rear Left'
        /// </summary>
        RearLeft,

        /// <summary>
        /// Microsoft calls this 'Back Right', Apple calls this 'Right Surround' (!), Dolby calls this 'Surround Rear Right'
        /// </summary>
        RearRight,

        /// <summary>
        /// Microsoft calls this 'Low Frequency', Apple calls this 'LFEScreen'
        /// </summary>
        Lfe,

        Subwoofer = Lfe,

        /// <summary>
        /// Apple, Dolby call this 'Left Center'
        /// </summary>
        FrontLeftOfCenter,

        /// <summary>
        /// Apple, Dolby call this 'Right Center'
        /// </summary>
        FrontRightOfCenter,

        /// <summary>
        /// Apple calls this 'Left Surround Direct', Dolby calls this 'Surround Left' (!)
        /// </summary>
        SideLeft,

        /// <summary>
        /// Apple calls this 'Right Surround Direct', Dolby calls this 'Surround Right' (!)
        /// </summary>
        SideRight,

        Aux0,
        Aux1,
        Aux2,
        Aux3,
        Aux4,
        Aux5,
        Aux6,
        Aux7,
        Aux8,
        Aux9,
        Aux10,
        Aux11,
        Aux12,
        Aux13,
        Aux14,
        Aux15,
        Aux16,
        Aux17,
        Aux18,
        Aux19,
        Aux20,
        Aux21,
        Aux22,
        Aux23,
        Aux24,
        Aux25,
        Aux26,
        Aux27,
        Aux28,
        Aux29,
        Aux30,
        Aux31,

        /// <summary>
        /// Apple calls this 'Top Center Surround'
        /// </summary>
        TopCenter,

        /// <summary>
        /// Apple calls this 'Vertical Height Left'
        /// </summary>
        TopFrontLeft,

        /// <summary>
        /// Apple calls this 'Vertical Height Right'
        /// </summary>
        TopFrontRight,

        /// <summary>
        /// Apple calls this 'Vertical Height Center'
        /// </summary>
        TopFrontCenter,

        /// <summary>
        /// Microsoft and Apple call this 'Top Back Left'
        /// </summary>
        TopRearLeft,

        /// <summary>
        /// Microsoft and Apple call this 'Top Back Right'
        /// </summary>
        TopRearRight,

        /// <summary>
        /// Microsoft and Apple call this 'Top Back Center'
        /// </summary>
        TopRearCenter,

        Max
    }
}