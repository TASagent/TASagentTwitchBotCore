using System.Diagnostics;
using System.Reflection;

namespace TASagentTwitchBot.Core.Logs;

public class LocalLogger : IDisposable
{
    private static string FileTimeStampA => DateTime.Now.ToString("yyyy-MM-dd");
    private static string FileTimeStampB => DateTime.Now.ToString("HH-mm-ss");
    private static string Header => $"%Version {VersionNumber}";

    private static string VersionNumber => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion!;

    private readonly StreamWriter logWriter;
    private bool disposedValue;

    public LocalLogger(string subDir, string fileName)
    {
        logWriter = File.CreateText(
            path: BGC.IO.DataManagement.PathForDataFile(subDir, $"{FileTimeStampA} {fileName} {FileTimeStampB}.txt"));
        PushLines(Header, "");
    }

    /// <summary>
    /// Append line to the log file
    /// </summary>
    public void PushLine(string line) => logWriter.WriteLine(line);

    /// <summary>
    /// Append lines to the log file
    /// </summary>
    public void PushLines(params string[] lines)
    {
        foreach (string line in lines)
        {
            logWriter.WriteLine(line);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                logWriter.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
