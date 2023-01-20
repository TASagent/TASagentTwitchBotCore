using TASagentTwitchBot.Core.DataForwarding;
using TASagentTwitchBot.Core.Audio;
using System.Linq;

namespace TASagentTwitchBot.Plugin.TTTAS;

public class DataForwardingTTTASHandler : IDataForwardingContextHandler
{
    private readonly ITTTASProvider tttasProvider;

    public DataForwardingTTTASHandler(
        ITTTASProvider tttasProvider)
    {
        this.tttasProvider = tttasProvider;
    }

    public List<ServerDataFile> GetDataFileList(string context)
    {
        if (context.ToUpper() != "TTTAS")
        {
            throw new Exception($"DataForwardingTTTASHandler received unexpected request context: {context}");
        }

        return tttasProvider
            .GetAllRecordings()
            .Select(x => new ServerDataFile(x.Name, new[] { x.Name }))
            .ToList();
    }

    public string? GetDataFilePath(string dataFileAlias, string context)
    {
        if (context.ToUpper() != "TTTAS")
        {
            throw new Exception($"DataForwardingTTTASHandler received unexpected request context: {context}");
        }

        return tttasProvider.GetRecordingFilePath(dataFileAlias);
    }

    public async Task Initialize(IDataForwardingInitializer initializer)
    {
        await initializer.ClearServerFileList("TTTAS");
        await initializer.UpdateServerFileList("TTTAS", GetDataFileList("TTTAS"));
    }

    void IDataForwardingContextHandler.Register(IDataForwardingRegistrar registrar) => registrar.RegisterHandler("TTTAS", this);
}
