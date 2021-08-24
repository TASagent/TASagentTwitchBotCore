using System;

namespace TASagentTwitchBot.Core.TTS
{
    public record AWSPollyCredentials(string AccessKey, string SecretKey);

    public record AzureSpeechSynthesisCredentials(string AccessKey, string Region);
}
