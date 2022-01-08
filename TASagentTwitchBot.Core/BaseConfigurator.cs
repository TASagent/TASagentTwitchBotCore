using NAudio.CoreAudioApi;

namespace TASagentTwitchBot.Core;

public abstract class BaseConfigurator : IConfigurator
{
    protected readonly Config.BotConfiguration botConfig;

    public BaseConfigurator(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        ErrorHandler errorHandler)
    {
        this.botConfig = botConfig;

        //Assign library log handlers
        BGC.Debug.ExceptionCallback += errorHandler.LogExternalException;

        BGC.Debug.LogCallback += communication.SendDebugMessage;
        BGC.Debug.LogWarningCallback += communication.SendWarningMessage;
        BGC.Debug.LogErrorCallback += communication.SendErrorMessage;
    }

    public abstract Task<bool> VerifyConfigured();

    protected void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\nERROR:   {message}\n");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    protected void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nWARNING: {message}\n");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    protected void WritePrompt(string message)
    {
        Console.Write($"Enter {message}:\n    > ");
    }

    protected void WriteMessage(string message)
    {
        Console.WriteLine(message);
    }

    protected bool ConfigureTwitchClient()
    {
        bool successful = false;

        if (string.IsNullOrEmpty(botConfig.TwitchClientId))
        {
            WritePrompt("Twitch Client ID received from https://dev.twitch.tv/console/apps ");

            string clientID = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(clientID))
            {
                botConfig.TwitchClientId = clientID;
                botConfig.Serialize();
            }
            else
            {
                WriteError("Empty Twitch ClientID received.");
                successful = false;
            }
        }

        if (string.IsNullOrEmpty(botConfig.TwitchClientSecret))
        {
            WritePrompt("Twitch Client Secret received from https://dev.twitch.tv/console/apps ");
            string clientSecret = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(clientSecret))
            {
                botConfig.TwitchClientSecret = clientSecret;
                botConfig.Serialize();
            }
            else
            {
                WriteError("Empty Twitch Client Secret received.");
                successful = false;
            }
        }

        return successful;
    }

    protected async Task<bool> ConfigureBroadcasterAccount(
        API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
        API.Twitch.HelixHelper helixHelper)
    {
        bool successful = true;

        if (string.IsNullOrEmpty(botConfig.Broadcaster))
        {
            WritePrompt("Broadcaster Username");

            string inputUserName = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(inputUserName))
            {
                botConfig.Broadcaster = inputUserName;
                botConfig.Serialize();
            }
            else
            {
                WriteError("No Broadcaster Username received.");
                successful = false;
            }
        }

        if (!string.IsNullOrEmpty(botConfig.TwitchClientId) &&
            !string.IsNullOrEmpty(botConfig.TwitchClientSecret) &&
            !string.IsNullOrEmpty(botConfig.Broadcaster))
        {
            if (await broadcasterTokenValidator.TryToConnect())
            {
                //Set broadcasterID if it's not set
                if (string.IsNullOrEmpty(botConfig.BroadcasterId))
                {
                    //Fetch the Broadcaster ID
                    API.Twitch.TwitchUsers? userData = await helixHelper.GetUsers(null, new List<string>() { botConfig.Broadcaster }) ??
                        throw new Exception("Unable to get User Data");

                    if (userData.Data is not null && userData.Data.Count > 0)
                    {
                        botConfig.BroadcasterId = userData.Data[0].ID;
                        botConfig.Serialize();
                    }
                    else
                    {
                        WriteError("Unable to fetch BroadcasterId.  Please check broadcaster credentials and try again.");
                        successful = false;
                    }
                }
            }
            else
            {
                WriteError("Unable to connect to Twitch.  Please check broadcaster credentials and try again.");
                successful = false;
            }
        }
        else
        {
            WriteWarning("Unable to fetch Broadcaster access tokens without ClientId, ClientSecret, and BroadcasterName. Skipping");
            successful = false;
        }

        return successful;
    }

    protected async Task<bool> ConfigureBotAccount(API.Twitch.IBotTokenValidator botTokenValidator)
    {
        bool successful = true;

        if (string.IsNullOrEmpty(botConfig.BotName))
        {
            WritePrompt("Bot Username");

            string inputUserName = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(inputUserName))
            {
                botConfig.BotName = inputUserName;
                botConfig.Serialize();
            }
            else
            {
                WriteError("Empty Bot Username received.");
                successful = false;
            }
        }

        if (!string.IsNullOrEmpty(botConfig.TwitchClientId) &&
            !string.IsNullOrEmpty(botConfig.TwitchClientSecret) &&
            !string.IsNullOrEmpty(botConfig.BotName))
        {
            if (await botTokenValidator.TryToConnect())
            {
                //Success
            }
            else
            {
                WriteError("Unable to connect to Twitch.  Please check bot credentials and try again.");
                successful = false;
            }
        }
        else
        {
            WriteWarning("Unable to fetch bot access tokens without ClientId, ClientSecret, and BotName. Skipping.");
            successful = false;
        }

        return successful;
    }


    protected bool ConfigurePasswords()
    {
        bool successful = true;
        if (string.IsNullOrEmpty(botConfig.AuthConfiguration.Admin.PasswordHash))
        {
            WritePrompt("Admin password for bot control");

            string pass = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(pass))
            {
                botConfig.AuthConfiguration.Admin.PasswordHash = Cryptography.HashPassword(pass);
                botConfig.Serialize();
            }
            else
            {
                WriteError("Empty Admin Password received.");
                successful = false;
            }
        }

        if (string.IsNullOrEmpty(botConfig.AuthConfiguration.Privileged.PasswordHash))
        {
            WritePrompt("Moderator password for bot control");

            string pass = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(pass))
            {
                botConfig.AuthConfiguration.Privileged.PasswordHash = Cryptography.HashPassword(pass);
                botConfig.Serialize();
            }
            else
            {
                WriteError("Empty Moderator Password received.");
                successful = false;
            }
        }

        if (string.IsNullOrEmpty(botConfig.AuthConfiguration.User.PasswordHash))
        {
            WritePrompt("User password for bot control");

            string pass = Console.ReadLine()!.Trim();

            if (!string.IsNullOrEmpty(pass))
            {
                botConfig.AuthConfiguration.User.PasswordHash = Cryptography.HashPassword(pass);
                botConfig.Serialize();
            }
            else
            {
                WriteError("Empty User Password received.");
                successful = false;
            }
        }

        return successful;
    }

    protected bool ConfigureAudioOutputDevices()
    {
        bool successful = true;

        //Set Audio Devices
        if (string.IsNullOrEmpty(botConfig.EffectOutputDevice) || string.IsNullOrEmpty(botConfig.VoiceOutputDevice))
        {
            List<string> devices = GetAudioOutputDevicesList();

            Console.WriteLine($"Detected, Active Output devices:");

            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"  {i}) {devices[i]}");
            }
            Console.WriteLine();

            if (string.IsNullOrEmpty(botConfig.EffectOutputDevice))
            {
                WritePrompt($"Default Effect Output Device Number");
                string inputLine = Console.ReadLine()!;
                Console.WriteLine();

                if (int.TryParse(inputLine, out int value))
                {
                    if (value >= 0 && value < devices.Count)
                    {
                        botConfig.EffectOutputDevice = devices[value];
                        botConfig.Serialize();
                    }
                    else
                    {
                        WriteError("Value out of range.");
                        successful = false;
                    }
                }
                else
                {
                    WriteError("Unable to parse value.");
                    successful = false;
                }

                Console.WriteLine();
            }

            if (string.IsNullOrEmpty(botConfig.VoiceOutputDevice))
            {
                WritePrompt($"Default Voice Output Device Number");
                string inputLine = Console.ReadLine()!;
                Console.WriteLine();

                if (int.TryParse(inputLine, out int value))
                {
                    if (value >= 0 && value < devices.Count)
                    {
                        botConfig.VoiceOutputDevice = devices[value];
                        botConfig.Serialize();
                    }
                    else
                    {
                        WriteError("Value out of range.");
                        successful = false;
                    }
                }
                else
                {
                    WriteError("Unable to parse value.");
                    successful = false;
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }

        return successful;
    }

    protected bool ConfigureAudioInputDevices()
    {
        bool successful = true;

        if (string.IsNullOrEmpty(botConfig.VoiceInputDevice))
        {
            List<string> devices = GetAudioInputDevicesList();

            Console.WriteLine($"Detected, Active Input devices:");

            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i}) {devices[i]}");
            }
            Console.WriteLine();

            WritePrompt("Default Voice Input Device Number");

            string inputLine = Console.ReadLine()!;
            Console.WriteLine();

            if (int.TryParse(inputLine, out int value))
            {
                if (value >= 0 && value < devices.Count)
                {
                    botConfig.VoiceInputDevice = devices[value];
                    botConfig.Serialize();
                }
                else
                {
                    Console.WriteLine("Value out of range.");
                    successful = false;
                }

            }
            else
            {
                WriteError("Unable to parse value.");
                successful = false;
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        return successful;
    }

    protected static List<string> GetAudioOutputDevicesList()
    {
        List<string> audioDevices = new List<string>();
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

        foreach (MMDevice audioDevice in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            audioDevices.Add(audioDevice.FriendlyName);
        }

        return audioDevices;
    }

    protected static List<string> GetAudioInputDevicesList()
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
