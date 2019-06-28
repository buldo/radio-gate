using System.Threading;
using System;

namespace MumbleSharp
{
    /// <summary>
    /// A basic mumble protocol which handles events from the server - override the individual handler methods to replace/extend the default behaviour
    /// </summary>
    public class BasicMumbleProtocol
    {
        public BasicMumbleProtocol(MumbleConnection connection)
        {

            //_encodingThread = new Thread(EncodingThreadEntry)
            //{
            //    IsBackground = true
            //};
        }

        //public void SendEncodedVoice(Channel channel, ArraySegment<byte> buffer, bool whisper = false)
        //{
        //    SendEncodedVoice(
        //        buffer,
        //        target: whisper ? SpeechTarget.WhisperToChannel : SpeechTarget.Normal,
        //        targetId: channel.Id
        //    );
        //}


        //public void Close()
        //{
        //    _encodingThread.Abort();

        //    _connection = null;
        //    LocalUser = null;
        //}

        #region voice

        //private void EncodingThreadEntry()
        //{
        //    IsEncodingThreadRunning = true;
        //    while (IsEncodingThreadRunning)
        //    {
        //        byte[] packet = null;
        //        try
        //        {
        //            packet = _encodingBuffer.Encode(TransmissionCodec);
        //        }
        //        catch
        //        {
        //        }

        //        if (packet != null)
        //        {
        //            int maxSize = 480;

        //            //taken from JS port
        //            for (int currentOffcet = 0; currentOffcet < packet.Length;)
        //            {
        //                int currentBlockSize = Math.Min(packet.Length - currentOffcet, maxSize);

        //                byte type = TransmissionCodec == SpeechCodec.Opus ? (byte) 4 : (byte) 0;
        //                //originaly [type = codec_type_id << 5 | whistep_chanel_id]. now we can talk only to normal chanel
        //                type = (byte) (type << 5);
        //                byte[] sequence = Var64.writeVarint64_alternative((UInt64) sequenceIndex);

        //                // Client side voice header.
        //                byte[] voiceHeader = new byte[1 + sequence.Length];
        //                voiceHeader[0] = type;
        //                sequence.CopyTo(voiceHeader, 1);

        //                byte[] header = Var64.writeVarint64_alternative((UInt64) currentBlockSize);
        //                byte[] packedData = new byte[voiceHeader.Length + header.Length + currentBlockSize];

        //                Array.Copy(voiceHeader, 0, packedData, 0, voiceHeader.Length);
        //                Array.Copy(header, 0, packedData, voiceHeader.Length, header.Length);
        //                Array.Copy(packet, currentOffcet, packedData, voiceHeader.Length + header.Length,
        //                    currentBlockSize);

        //                _connection.SendEncodedVoice(new ArraySegment<byte>(packedData));

        //                sequenceIndex++;
        //                currentOffcet += currentBlockSize;
        //            }
        //        }

        //        //beware! can take a lot of power, because infinite loop without sleep
        //    }
        //}

        //public void SendEncodedVoice(ArraySegment<byte> pcm, SpeechTarget target, uint targetId)
        //{
        //    _encodingBuffer.Add(pcm, target, targetId);
        //}

        //public void SendVoiceStop()
        //{
        //    _encodingBuffer.Stop();
        //    sequenceIndex = 0;
        //}

        #endregion
    }
}
