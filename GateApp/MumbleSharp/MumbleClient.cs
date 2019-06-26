using System;
using System.Collections.Generic;
using System.Text;
using MumbleSharp.Services;

namespace MumbleSharp
{
    public class MumbleClient
    {
        private readonly HashSet<IService> _services; // TODO заменить на контейнер
        private readonly IVoicePacketProcessor _voicePacketProcessor;
        private readonly MumbleConnection _connection;

        public MumbleClient(
            MumbleConnection connection,
            HashSet<IService> services,
            IVoicePacketProcessor voicePacketProcessor
            ) // TODO: заменить на IServiceCollection
        {
            _services = services;
            _voicePacketProcessor = voicePacketProcessor;
            _connection = connection;
        }

        public void Start()
        {
            foreach (var service in _services)
            {
                foreach (var processor in service.GetProcessors())
                {
                    _connection.RegisterPacketProcessor(processor);
                }
            }

            _connection.RegisterVoicePacketProcessor(_voicePacketProcessor);
        }
    }
}
