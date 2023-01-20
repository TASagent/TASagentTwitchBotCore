namespace TASagentTwitchBot.Core.Audio;

public record ServerDataFile(string Name, string[] Aliases);
public record ServerFileData(byte[] Data, string? ContentType);