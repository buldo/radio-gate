using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Gate.Radio.Emulator
{
    internal class RadioStateService
    {
        public TxState TxState { get; private set; }

        public RxState RxState { get; private set; }

        public Action OnStateChangedAction { get; set; }
        
        //private Timer _timer = new Timer(1000);

        //public RadioStateService()
        //{
        //    _timer.Elapsed += (sender, args) =>
        //    {
        //        if (TxState == TxState.Idle)
        //        {
        //            StartTx();
        //        }
        //        else
        //        {
        //            StopTx();
        //        }
        //    };
        //    _timer.AutoReset = true;
        //    _timer.Enabled = true;
        //}

        public void StartTx()
        {
            TxState = TxState.Tx;
            OnStateChanged();
        }

        public void StopTx()
        {
            TxState = TxState.Idle;
            OnStateChanged();
        }

        protected virtual void OnStateChanged()
        {
            OnStateChangedAction?.Invoke();
        }
    }
}
