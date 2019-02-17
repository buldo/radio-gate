using System;
using NAudio.Pulse;
using NAudio.Wave;

namespace MumbleSharp.Demo
{
    public class MicrophoneRecorder
    {
        private readonly BasicMumbleProtocol _protocol;

        private bool _recording = true;

        public MicrophoneRecorder(BasicMumbleProtocol protocol)
        {
            _protocol = protocol;
            IWaveIn sourceStream;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                sourceStream = new WaveInEventPulse(new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Record"));
            }
            else
            {
                sourceStream = new WaveInEvent();
            }

            sourceStream.WaveFormat = new WaveFormat(Constants.SAMPLE_RATE, Constants.SAMPLE_BITS, Constants.CHANNELS);

            sourceStream.DataAvailable += VoiceDataAvailable;

            sourceStream.StartRecording();
        }

        private void VoiceDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_recording)
                return;

            //At the moment we're sending *from* the local user, this is kinda stupid.
            //What we really want is to send *to* other users, or to channels. Something like:
            //
            //    _connection.Users.First().SendVoiceWhisper(e.Buffer);
            //
            //    _connection.Channels.First().SendVoice(e.Buffer, shout: true);

            //if (_protocol.LocalUser != null)
            //    _protocol.LocalUser.SendVoice(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));

            //Send to the channel LocalUser is currently in
            if (_protocol.LocalUser != null && _protocol.LocalUser.Channel != null)
                _protocol.SendVoice(_protocol.LocalUser.Channel, new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded));
        }

        public void Record()
        {
            _recording = true;
        }

        public void Stop()
        {
            _recording = false;
            _protocol.SendVoiceStop();
        }
    }
}
