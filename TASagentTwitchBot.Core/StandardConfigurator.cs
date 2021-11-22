namespace TASagentTwitchBot.Core;

public class StandardConfigurator : BaseConfigurator
{
    protected readonly API.Twitch.HelixHelper helixHelper;
    protected readonly API.Twitch.IBotTokenValidator botTokenValidator;
    protected readonly API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator;

    public StandardConfigurator(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        ErrorHandler errorHandler,
        API.Twitch.HelixHelper helixHelper,
        API.Twitch.IBotTokenValidator botTokenValidator,
        API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator)
        : base(botConfig, communication, errorHandler)
    {
        this.helixHelper = helixHelper;
        this.botTokenValidator = botTokenValidator;
        this.broadcasterTokenValidator = broadcasterTokenValidator;
    }

    public override async Task<bool> VerifyConfigured()
    {
        bool successful = true;

        //Client Information
        successful |= ConfigureTwitchClient();

        //Check Accounts
        successful |= await ConfigureBroadcasterAccount(broadcasterTokenValidator, helixHelper);
        successful |= await ConfigureBotAccount(botTokenValidator);

        successful |= ConfigurePasswords();

        successful |= ConfigureAudioOutputDevices();
        successful |= ConfigureAudioInputDevices();

        return successful;
    }
}
