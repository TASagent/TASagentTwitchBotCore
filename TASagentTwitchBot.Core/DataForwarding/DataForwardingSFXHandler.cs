using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.DataForwarding;

public class DataForwardingSFXHandler : IDataForwardingContextHandler
{
    private readonly ISoundEffectSystem soundEffectSystem;

    public DataForwardingSFXHandler(
        ISoundEffectSystem soundEffectSystem)
    {
        this.soundEffectSystem = soundEffectSystem;
    }

    public List<ServerDataFile> GetDataFileList(string context)
    {
        if (context.ToUpper() != "SFX")
        {
            throw new Exception($"DataForwardingSFXHandler received unexpected request context: {context}");
        }

        return soundEffectSystem
            .GetAllSoundEffects()
            .Select(x => new ServerDataFile(x.Name, x.Aliases))
            .ToList();
    }

    public string? GetDataFilePath(string dataFileAlias, string context)
    {
        if (context.ToUpper() != "SFX")
        {
            throw new Exception($"DataForwardingSFXHandler received unexpected request context: {context}");
        }

        SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByAlias(dataFileAlias);

        if (soundEffect is null)
        {
            return null;
        }

        return soundEffect.FilePath;
    }

    public async Task Initialize(IDataForwardingInitializer initializer)
    {
        await initializer.ClearServerFileList("SFX");
        await initializer.UpdateServerFileList("SFX", GetDataFileList("SFX"));
    }

    void IDataForwardingContextHandler.Register(IDataForwardingRegistrar registrar) => registrar.RegisterHandler("SFX", this);
}
