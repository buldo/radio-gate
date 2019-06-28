using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace MumbleSharp.Voice.Tests
{
    [TestClass]
    public class PlaybackTest
    {
        [TestMethod]
        [Ignore]
        public void PlayWav()
        {
            var fileReader = new WaveFileReader("Content/test.wav");
            var waveOut = new WaveOutEvent();
            waveOut.Init(fileReader);
            var evnt = new ManualResetEventSlim(false);
            waveOut.PlaybackStopped += (sender, args) => evnt.Set();
            waveOut.Play();
            evnt.Wait();
        }
    }
}
