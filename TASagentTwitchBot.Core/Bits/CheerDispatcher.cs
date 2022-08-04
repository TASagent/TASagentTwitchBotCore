namespace TASagentTwitchBot.Core.Bits;

public class CheerDispatcher : IStartupListener
{
    private readonly TTS.TTSConfiguration ttsConfig;
    private readonly Notifications.ICheerHandler cheerHandler;

    public CheerDispatcher(
        TTS.TTSConfiguration ttsConfig,
        ICommunication communication,
        Notifications.ICheerHandler cheerHandler)
    {
        this.ttsConfig = ttsConfig;

        this.cheerHandler = cheerHandler;

        communication.ReceiveMessageHandlers += CheerMessageHandler;
    }

    private void CheerMessageHandler(IRC.TwitchChatter chatter)
    {
        if (chatter.Bits != 0)
        {
            bool meetsTTSThreshold = chatter.Bits >= ttsConfig.BitThreshold;

            cheerHandler.HandleCheer(chatter.User, chatter.Message, chatter.Bits, meetsTTSThreshold, true);
        }
    }
}
