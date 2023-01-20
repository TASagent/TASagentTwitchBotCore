namespace TASagentTwitchBot.Core.Audio;

public record ServerSoundEffect(string Name, string[] Aliases);
public record ServerSoundEffectData(string Name, byte[] Data, string? ContentType);