//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    sealed public class DelayedControllerReader : IControllerReader, IDisposable
//    {
//        private readonly IControllerReader baseControllerReader;
//        private readonly int delayInMilliseconds;

//        public event EventHandler ControllerDisconnected;
//        public event StateEventHandler ControllerStateChanged;

//        public IControllerReader BaseControllerReader => baseControllerReader;
//        public int DelayInMilliseconds => delayInMilliseconds;

//        public DelayedControllerReader(IControllerReader baseControllerReader, int delayInMilliseconds)
//        {
//            this.baseControllerReader = baseControllerReader;
//            this.delayInMilliseconds = delayInMilliseconds;

//            BaseControllerReader.ControllerStateChanged += BaseControllerReader_ControllerStateChanged;
//        }

//        private async void BaseControllerReader_ControllerStateChanged(IControllerReader sender, ControllerState state)
//        {
//            if (!disposedValue)
//            {
//                await Task.Delay(delayInMilliseconds);

//                StateEventHandler controllerStateChanged = ControllerStateChanged;
//                if (controllerStateChanged != null)
//                {
//                    controllerStateChanged(this, state);
//                }
//            }
//        }

//        #region IDisposable Support
//        private bool disposedValue = false; // To detect redundant calls

//        void Dispose(bool disposing)
//        {
//            if (!disposedValue)
//            {
//                if (disposing)
//                {
//                    BaseControllerReader.Dispose();
//                    BaseControllerReader.ControllerStateChanged -= BaseControllerReader_ControllerStateChanged;
//                }

//                disposedValue = true;
//            }
//        }

//        public void Dispose()
//        {
//            Dispose(true);
//        }
//        #endregion
//    }
//}
