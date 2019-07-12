using Gate.Radio;
using MumbleSharp.Voice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Daemon
{
    internal class GateMediator
    {
        private readonly IRadioStation _radioStation;
        private readonly VoiceService _voiceService;

        public GateMediator(IRadioStation radioStation, VoiceService voiceService)
        {
            _radioStation = radioStation;
            _radioStation.StateChanged += RadioStationOnStateChanged;

            _voiceService = voiceService;
            _voiceService.UsersStateChanged += VoiceServiceOnUsersStateChanged;
        }

        private void VoiceServiceOnUsersStateChanged(object sender, EventArgs e)
        {
            CalculateTx();
        }

        private void RadioStationOnStateChanged(object sender, RadioStateChangedEventArgs e)
        {
            if(e.NewState == RadioState.Rx && e.OldState != RadioState.Rx)
            {
                // Начинаем отправку на сервер
                _voiceService.StartSendingVoice();
            }

            if(e.NewState != RadioState.Rx && e.OldState == RadioState.Rx)
            {
                _voiceService.StopSendingVoice();
                CalculateTx();
            }
        }

        private void CalculateTx()
        {
            if(_radioStation.State == RadioState.Rx)
            {
                return; // Станция принимает, мы отправляем это на сервер. Ничего не поделать
            }

            var voiceFromServerAvailable = _voiceService.GetUsersState().Any(s => s.State == UserVoiceState.Tx);
            if(voiceFromServerAvailable)
            {
                _radioStation.StartTransceiving();
            }
            else
            {
                _radioStation.StopTransceiving();
            }
        }
    }
}
