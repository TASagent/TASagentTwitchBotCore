using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using BGC.IO;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.BotConfigurator
{
    public class BotConfigurator
    {
        private readonly Core.ICommunication communication;
        private readonly Core.Config.IBotConfigContainer botConfigContainer;
        private readonly HelixHelper helixHelper;
        private readonly IBotTokenValidator botTokenValidator;
        private readonly IBroadcasterTokenValidator broadcasterTokenValidator;

        public BotConfigurator(
            Core.Config.IBotConfigContainer botConfigContainer,
            Core.ICommunication communication,
            Core.ErrorHandler errorHandler,
            IBotTokenValidator botTokenValidator,
            IBroadcasterTokenValidator broadcasterTokenValidator)
        {
            this.botConfigContainer = botConfigContainer;
            this.communication = communication;

            this.botTokenValidator = botTokenValidator;
            this.broadcasterTokenValidator = broadcasterTokenValidator;

            BGC.Debug.ExceptionCallback += errorHandler.LogExternalException;

            //Assign library log handlers
            BGC.Debug.LogCallback += communication.SendDebugMessage;
            BGC.Debug.LogWarningCallback += communication.SendWarningMessage;
            BGC.Debug.LogErrorCallback += communication.SendErrorMessage;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Welcome to the TASagentBot Configurator!\n\n");

            BGC.Debug.LogCallback += communication.SendDebugMessage;
            BGC.Debug.LogWarningCallback += communication.SendWarningMessage;
            BGC.Debug.LogErrorCallback += communication.SendErrorMessage;

            Core.Config.BotConfiguration botConfig = botConfigContainer.BotConfig;
            botConfigContainer.SerializeData();

            //Basic Setup

            if (string.IsNullOrEmpty(botConfig.BotName))
            {
                Console.Write("Enter the Bot's Username:\n    > ");
                string inputUserName = Console.ReadLine();

                inputUserName = inputUserName.Trim();

                if (string.IsNullOrEmpty(inputUserName))
                {
                    Console.WriteLine("\n\nError: Empty Bot Username received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.BotName = inputUserName;

                botConfigContainer.SerializeData();
            }

            if (string.IsNullOrEmpty(botConfig.Broadcaster))
            {
                Console.Write("Enter the Broadcaster's Username:\n    > ");
                string inputUserName = Console.ReadLine();

                inputUserName = inputUserName.Trim();

                if (string.IsNullOrEmpty(inputUserName))
                {
                    Console.WriteLine("\n\nError: Empty Broadcaster Username received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.Broadcaster = inputUserName;

                botConfigContainer.SerializeData();
            }

            //Secure portion

            if (string.IsNullOrEmpty(botConfig.TwitchClientId))
            {
                Console.Write("Enter the Twitch Client ID received from https://dev.twitch.tv/console/apps \n    > ");
                string clientID = Console.ReadLine();

                clientID = clientID.Trim();

                if (string.IsNullOrEmpty(clientID))
                {
                    Console.WriteLine("\n\nError: Empty Twitch ClientID received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.TwitchClientId = clientID;

                botConfigContainer.SerializeData();
            }

            if (string.IsNullOrEmpty(botConfig.TwitchClientSecret))
            {
                Console.Write("Enter the Twitch Client Secret received from https://dev.twitch.tv/console/apps \n    > ");
                string clientSecret = Console.ReadLine();

                clientSecret = clientSecret.Trim();

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Console.WriteLine("\n\nError: Empty Twitch Client Secret received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.TwitchClientSecret = clientSecret;

                botConfigContainer.SerializeData();
            }

            //Try to connect and validate tokens
            if (await botTokenValidator.TryToConnect())
            {
                Console.WriteLine("Bot Token successfully validates");
            }
            else
            {
                Console.WriteLine("Unable to connect to Twitch");
                Console.WriteLine("Please check bot credentials and try again.");
                Console.WriteLine("Exiting bot configurator now...");
                Environment.Exit(1);
            }

            if (await broadcasterTokenValidator.TryToConnect())
            {
                Console.WriteLine("Broadcaster Token successfully validates");
            }
            else
            {
                Console.WriteLine("Unable to connect to Twitch");
                Console.WriteLine("Please check broadcaster credentials and try again.");
                Console.WriteLine("Exiting bot configurator now...");
                Environment.Exit(1);
            }

            //Now we have tokens

            //Set broadcasterID if it's not set
            if (string.IsNullOrEmpty(botConfig.BroadcasterId))
            {
                //Fetch the Broadcaster ID
                botConfig.BroadcasterId = (await helixHelper.GetUsers(null, new List<string>() { botConfig.Broadcaster })).Data[0].ID;

                botConfigContainer.SerializeData();
            }

            if (string.IsNullOrEmpty(botConfig.AuthConfiguration.Admin.Password))
            {
                Console.Write("Enter Admin password you'd like to use for bot control:\n    > ");
                string pass = Console.ReadLine();

                pass = pass.Trim();

                if (string.IsNullOrEmpty(pass))
                {
                    Console.WriteLine("\n\nError: Empty Admin Password received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.AuthConfiguration.Admin.Password = pass;

                botConfigContainer.SerializeData();
            }

            if (string.IsNullOrEmpty(botConfig.AuthConfiguration.Privileged.Password))
            {
                Console.Write("Enter Moderator password you'd like to use for bot control:\n    > ");
                string pass = Console.ReadLine();

                pass = pass.Trim();

                if (string.IsNullOrEmpty(pass))
                {
                    Console.WriteLine("\n\nError: Empty Moderator Password received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.AuthConfiguration.Privileged.Password = pass;

                botConfigContainer.SerializeData();
            }

            if (string.IsNullOrEmpty(botConfig.AuthConfiguration.User.Password))
            {
                Console.Write("Enter User password you'd like to use for bot control:\n    > ");
                string pass = Console.ReadLine();

                pass = pass.Trim();

                if (string.IsNullOrEmpty(pass))
                {
                    Console.WriteLine("\n\nError: Empty User Password received. Aborting.");
                    Environment.Exit(1);
                }

                botConfig.AuthConfiguration.User.Password = pass;

                botConfigContainer.SerializeData();
            }

            //Set Audio Devices
            if (string.IsNullOrEmpty(botConfig.EffectOutputDevice) || string.IsNullOrEmpty(botConfig.VoiceOutputDevice))
            {
                List<string> devices = GetAudioOutputDevicesList();

                Console.WriteLine($"Detected, Active Output devices:");

                for (int i = 0; i < devices.Count; i++)
                {
                    Console.WriteLine($"{i}) {devices[i]}");
                }
                Console.WriteLine();

                if (string.IsNullOrEmpty(botConfig.EffectOutputDevice))
                {
                    Console.Write($"Enter Effect Output Device number: > ");
                    string inputLine = Console.ReadLine();
                    if (!int.TryParse(inputLine, out int value))
                    {
                        Console.WriteLine("Unable to parse value. Aborting");
                        Environment.Exit(1);
                    }

                    if (value < 0 || value >= devices.Count)
                    {
                        Console.WriteLine("Value out of range. Aborting");
                        Environment.Exit(1);
                    }

                    botConfig.EffectOutputDevice = devices[value];
                    botConfigContainer.SerializeData();
                }

                if (string.IsNullOrEmpty(botConfig.VoiceOutputDevice))
                {
                    Console.Write($"Enter Voice Output Device number: > ");
                    string inputLine = Console.ReadLine();
                    if (!int.TryParse(inputLine, out int value))
                    {
                        Console.WriteLine("Unable to parse value. Aborting");
                        Environment.Exit(1);
                    }

                    if (value < 0 || value >= devices.Count)
                    {
                        Console.WriteLine("Value out of range. Aborting");
                        Environment.Exit(1);
                    }

                    botConfig.VoiceOutputDevice = devices[value];
                    botConfigContainer.SerializeData();
                }

                Console.WriteLine();
                Console.WriteLine();
            }

            if (string.IsNullOrEmpty(botConfig.VoiceInputDevice))
            {
                List<string> devices = GetAudioInputDevicesList();

                Console.WriteLine($"Detected, Active Input devices:");

                for (int i = 0; i < devices.Count; i++)
                {
                    Console.WriteLine($"{i}) {devices[i]}");
                }
                Console.WriteLine();

                Console.Write($"Enter Voice Input Device number: > ");
                string inputLine = Console.ReadLine();
                Console.WriteLine();
                if (!int.TryParse(inputLine, out int value))
                {
                    Console.WriteLine("Unable to parse value. Aborting");
                    Environment.Exit(1);
                }

                if (value < 0 || value >= devices.Count)
                {
                    Console.WriteLine("Value out of range. Aborting");
                    Environment.Exit(1);
                }

                botConfig.VoiceInputDevice = devices[value];
                botConfigContainer.SerializeData();

                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine("Configurator has completed");
        }

        private static List<string> GetAudioOutputDevicesList()
        {
            List<string> audioDevices = new List<string>();
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            foreach (MMDevice audioDevice in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                audioDevices.Add(audioDevice.FriendlyName);
            }

            return audioDevices;
        }

        private static List<string> GetAudioInputDevicesList()
        {
            List<string> audioDevices = new List<string>();
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            foreach (MMDevice audioDevice in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                audioDevices.Add(audioDevice.FriendlyName);
            }

            return audioDevices;
        }
    }
}
