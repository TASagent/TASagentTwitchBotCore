namespace TASagentTwitchBot.Core.Audio;

public record ServerSoundEffect(string Name, string[] Aliases);
public record ServerSoundEffectData(byte[] Data, string? ContentType);