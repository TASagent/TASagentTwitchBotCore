//using System;
//using System.Collections.Generic;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    public class BlinkReductionFilter
//    {
//        private bool buttonEnabled = false;
//        private bool analogEnabled = false;
//        private bool massEnabled = false;

//        private readonly List<ControllerState> states = new List<ControllerState>();
//        private ControllerState lastUnfiltered = ControllerState.Zero;

//        public BlinkReductionFilter()
//        {
//            states.Add(ControllerState.Zero);
//            states.Add(ControllerState.Zero);
//            states.Add(ControllerState.Zero);
//        }

//        public void SetState(BlinkReductionState blinkReductionState)
//        {
//            buttonEnabled = blinkReductionState.ButtonEnabled;
//            analogEnabled = blinkReductionState.AnalogEnabled;
//            massEnabled = blinkReductionState.MassEnabled;
//        }

//        public BlinkReductionState GetState() => new BlinkReductionState(
//            ButtonEnabled: buttonEnabled,
//            AnalogEnabled: analogEnabled,
//            MassEnabled: massEnabled);

//        public ControllerState Process(ControllerState state)
//        {
//            if (!buttonEnabled && !analogEnabled && !massEnabled)
//            {
//                return state;
//            }

//            bool revert = false;
//            bool filtered = false;

//            //move by one frame
//            states.RemoveAt(0);
//            states.Add(state);

//            ControllerStateBuilder filteredStateBuilder = new ControllerStateBuilder();

//            uint massCounter = 0;
//            foreach (string button in states[0].Buttons.Keys)
//            {
//                filteredStateBuilder.SetButton(button, states[2].Buttons[button]);

//                if (buttonEnabled)
//                {
//                    // previous previous frame equals current frame
//                    // AND current frame not equals previous frame
//                    if (states[0].Buttons[button] == states[2].Buttons[button] &&
//                        states[2].Buttons[button] != states[1].Buttons[button])
//                    {
//                        // if noisy, we turn the button off
//                        filteredStateBuilder.SetButton(button, false);
//                        filtered = true;
//                    }
//                }

//                if (massEnabled)
//                {
//                    if (states[2].Buttons[button])
//                    {
//                        massCounter++;
//                    }
//                }
//            }

//            foreach (string button in states[0].Analogs.Keys)
//            {
//                filteredStateBuilder.SetAnalog(button, states[2].Analogs[button]);
//                if (massEnabled)
//                {
//                    if (Math.Abs(Math.Abs(states[2].Analogs[button]) - Math.Abs(states[1].Analogs[button])) > 0.3)
//                    {
//                        massCounter++;
//                    }
//                }

//                if (analogEnabled)
//                {
//                    // If we traveled over 0.5 Analog between the last three frames
//                    // but less than 0.1 in the frame before
//                    // we drop the change for this input
//                    if (Math.Abs(states[2].Analogs[button] - states[1].Analogs[button]) > .5f &&
//                        Math.Abs(states[1].Analogs[button] - states[0].Analogs[button]) < 0.1f)
//                    {
//                        filteredStateBuilder.SetAnalog(button, lastUnfiltered.Analogs[button]);
//                        filtered = true;
//                    }
//                }
//            }

//            // if over 80% of the buttons are used we revert (this is either a reset button combo or a blink)
//            if (massCounter > (states[0].Analogs.Count + states[0].Buttons.Count) * 0.8)
//            {
//                revert = true;
//            }

//            if (revert)
//            {
//                return lastUnfiltered;
//            }

//            if (filtered)
//            {
//                return filteredStateBuilder.Build();
//            }

//            lastUnfiltered = states[2];
//            return states[2];
//        }
//    }

//    public record BlinkReductionState(bool ButtonEnabled, bool AnalogEnabled, bool MassEnabled);
//}
