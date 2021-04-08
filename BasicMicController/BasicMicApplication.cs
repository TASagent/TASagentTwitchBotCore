using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicMicController
{
    public class BasicMicApplication
    {
        private readonly TASagentTwitchBot.Core.ICommunication communication;
        private readonly TASagentTwitchBot.Core.ErrorHandler errorHandler;
        private readonly TASagentTwitchBot.Core.ApplicationManagement applicationManagement;
        private readonly TASagentTwitchBot.Core.IMessageAccumulator messageAccumulator;

        private readonly TASagentTwitchBot.Core.Audio.IMicrophoneHandler microphoneHandler;

        private readonly TASagentTwitchBot.Core.Audio.MidiKeyboardHandler midiKeyboardHandler;

        public BasicMicApplication(
            TASagentTwitchBot.Core.ErrorHandler errorHandler,
            TASagentTwitchBot.Core.ApplicationManagement applicationManagement,
            TASagentTwitchBot.Core.IMessageAccumulator messageAccumulator,
            TASagentTwitchBot.Core.Audio.IMicrophoneHandler microphoneHandler,
            TASagentTwitchBot.Core.ICommunication communication,
            TASagentTwitchBot.Core.Audio.MidiKeyboardHandler midiKeyboardHandler)
        {
            this.microphoneHandler = microphoneHandler;
            this.errorHandler = errorHandler;
            this.applicationManagement = applicationManagement;
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;
            this.midiKeyboardHandler = midiKeyboardHandler;

            BGC.Debug.ExceptionCallback += errorHandler.LogExternalException;

            //Assign library log handlers
            BGC.Debug.LogCallback += communication.SendDebugMessage;
            BGC.Debug.LogWarningCallback += communication.SendWarningMessage;
            BGC.Debug.LogErrorCallback += communication.SendErrorMessage;
        }

        public async Task RunAsync()
        {
            try
            {
                communication.SendDebugMessage("*** Starting Up Basic Mic Application ***");

                microphoneHandler.Start();
            }
            catch (Exception ex)
            {
                errorHandler.LogFatalException(ex);
            }

            messageAccumulator.MonitorMessages();

            try
            {
                await applicationManagement.WaitForEndAsync();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }


            //Handle Cleanup
            try
            {
                microphoneHandler.Dispose();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }

            try
            {
                midiKeyboardHandler.Dispose();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
        }
    }
}
