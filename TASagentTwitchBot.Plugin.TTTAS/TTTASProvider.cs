using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Plugin.TTTAS;

[AutoRegister]
public interface ITTTASProvider
{
    int GetRecordingCount();
    int GetPendingCount();

    bool ShowPrompt();
    string GetPreppedWord();
    void StartRecording();
    void EndRecording();

    void CancelCurrentPrompt();

    void ClearPrompt();

    List<string> GetPendingRecordings();

    void Rerecord(string words);

    AudioRequest GetWord(string word, string requestId, Effect? effect = null);
}

public class TTTASProvider : ITTTASProvider
{
    private readonly ICommunication communication;
    private readonly IMicrophoneHandler microphoneHandler;
    private readonly IHubContext<Web.Hubs.TTTASHub> monitorHubContext;

    private static string TTTASFilesPath => BGC.IO.DataManagement.PathForDataDirectory("TTTASFiles");

    private readonly RecordingData recordingData;
    private readonly string dataFilePath;
    private readonly Random randomizer = new Random();

    private readonly Dictionary<string, PendingRecording> pendingRecordings = new Dictionary<string, PendingRecording>();
    private PendingRecording? preppedWord = null;

    private bool recording = false;

    public TTTASProvider(
        ICommunication communication,
        IMicrophoneHandler microphoneHandler,
        IHubContext<Web.Hubs.TTTASHub> monitorHubContext)
    {
        this.communication = communication;
        this.microphoneHandler = microphoneHandler;
        this.monitorHubContext = monitorHubContext;

        dataFilePath = BGC.IO.DataManagement.PathForDataFile("Config", "TTTAS", "TTTASData.json");

        if (File.Exists(dataFilePath))
        {
            recordingData = JsonSerializer.Deserialize<RecordingData>(File.ReadAllText(dataFilePath))!;
            recordingData.VerifyAndPopulate(communication);
        }
        else
        {
            recordingData = new RecordingData();
            File.WriteAllText(dataFilePath, JsonSerializer.Serialize(recordingData));
        }
    }

    /// <summary>
    /// Get the AudioRequest of the TTTAS word.  Optionally request a recording of it if it does not already exist.
    /// </summary>
    public AudioRequest GetWord(
        string word,
        string requestId,
        Effect? effect = null)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            communication.SendWarningMessage($"TTTAS Word Request of null or whitespace.");
            return new AudioDelay(500);
        }

        if (effect is null)
        {
            effect = new NoEffect();
        }

        word = word.Trim().ToLowerInvariant();

        if (word.EndsWith('.') || word.EndsWith(','))
        {
            //Strip off trailing periods and commas.
            word = word[..^1];
        }

        if (recordingData.RecordingLookup.TryGetValue(word, out Recording? recording))
        {
            //Return the word
            return new AudioFileRequest(recording.FilePath, true, effect);
        }

        //Request recording
        lock (pendingRecordings)
        {
            if (recordingData.RecordingLookup.TryGetValue(word, out recording))
            {
                //Return the word
                return new AudioFileRequest(recording.FilePath, true, effect);
            }

            if (!pendingRecordings.TryGetValue(word, out PendingRecording? pendingRecording))
            {
                string fileName = word
                    .Replace("'", "_apos_")
                    .Replace("?", "_ques_")
                    .Replace("!", "_bang_");

                //Truncate fileName to first 20 characters
                if (fileName.Length > 20)
                {
                    fileName = fileName[0..20];
                }

                string filePath = Path.Combine(TTTASFilesPath, $"{fileName}_{Guid.NewGuid()}.mp3");

                pendingRecording = new PendingRecording(word, filePath, requestId);
                pendingRecordings.Add(word, pendingRecording);
            }

            return new TTTASPendingAudioRequest(pendingRecording, effect);
        }
    }

    public int GetPendingCount() => pendingRecordings.Count;

    public string GetPreppedWord()
    {
        if (preppedWord is null)
        {
            return "";
        }

        return preppedWord.Name;
    }

    public bool ShowPrompt()
    {
        if (pendingRecordings.Count == 0)
        {
            return false;
        }

        int nextIndex = randomizer.Next(0, pendingRecordings.Count);
        preppedWord = pendingRecordings.Values.Skip(nextIndex).First();

        monitorHubContext.Clients.All.SendAsync("ReceivePrompt", preppedWord.Name);
        return true;
    }

    public void StartRecording()
    {
        if (preppedWord is null)
        {
            return;
        }

        recording = true;

        microphoneHandler.RecordVoiceStream(preppedWord.FilePath);
    }

    public void EndRecording()
    {
        if (preppedWord is null)
        {
            return;
        }

        if (!recording)
        {
            return;
        }

        recording = false;

        microphoneHandler.StopRecordingVoiceStream();

        lock (pendingRecordings)
        {
            if (!pendingRecordings.TryGetValue(preppedWord.Name, out PendingRecording? pendingRecording))
            {
                communication.SendErrorMessage($"Marked non-pending word \"{preppedWord.Name}\" as complete.");
                return;
            }

            pendingRecordings.Remove(preppedWord.Name);

            if (recordingData.RecordingLookup.TryGetValue(preppedWord.Name, out Recording? recording))
            {
                communication.SendWarningMessage($"Overriding existing word \"{preppedWord.Name}\".");

                recordingData.Recordings.Remove(recording);
                recordingData.RecordingLookup.Remove(preppedWord.Name);
            }

            recordingData.AddRecording(new Recording(preppedWord.Name, pendingRecording.FilePath));

            pendingRecording.MarkReady();
            pendingRecording = null;

            File.WriteAllText(dataFilePath, JsonSerializer.Serialize(recordingData));
        }

        if (!ShowPrompt())
        {
            ClearPrompt();
        }
    }

    public void CancelCurrentPrompt()
    {
        if (preppedWord is null)
        {
            //Can only cancel a prompt if one is selected
            return;
        }

        if (recording)
        {
            //End any ongoing recording
            recording = false;
            microphoneHandler.StopRecordingVoiceStream();
            ClearPrompt();
        }

        lock (pendingRecordings)
        {
            string requestId = preppedWord.RequestId;
            preppedWord.AbortRecording();
            pendingRecordings.Remove(preppedWord.Name);
            preppedWord = null;

            foreach (PendingRecording pendingRecording in pendingRecordings.Values.Where(x => x.RequestId == requestId).ToList())
            {
                pendingRecording.AbortRecording();
                pendingRecordings.Remove(pendingRecording.Name);
            }
        }

        if (!ShowPrompt())
        {
            ClearPrompt();
        }
    }

    public void Rerecord(string words)
    {
        string[] wordList = words.Trim().ToLowerInvariant().Split(' ');
        string newRequestId = Guid.NewGuid().ToString();

        foreach (string rerecordWord in wordList)
        {
            string word = rerecordWord;
            if (word.EndsWith('.') || word.EndsWith(','))
            {
                //Strip off trailing periods and commas.
                word = word[..^1];
            }

            //Request recording
            lock (pendingRecordings)
            {
                if (pendingRecordings.ContainsKey(word))
                {
                    //Word already pending
                    continue;
                }

                string fileName = word
                    .Replace("'", "_apos_")
                    .Replace("?", "_ques_")
                    .Replace("!", "_bang_");

                //Truncate fileName to first 20 characters
                if (fileName.Length > 20)
                {
                    fileName = fileName[0..20];
                }

                string filePath = Path.Combine(TTTASFilesPath, $"{fileName}_{Guid.NewGuid()}.mp3");

                pendingRecordings.Add(word, new PendingRecording(word, filePath, newRequestId));
            }
        }
    }

    public void ClearPrompt() => monitorHubContext.Clients.All.SendAsync("ClearPrompt");
    public int GetRecordingCount() => recordingData.Recordings.Count;

    public List<string> GetPendingRecordings() => pendingRecordings.Values.Select(x => x.Name).ToList();

    private class RecordingData
    {
        public List<Recording> Recordings { get; init; } = new List<Recording>();

        [JsonIgnore]
        public Dictionary<string, Recording> RecordingLookup { get; } = new Dictionary<string, Recording>();


        public void VerifyAndPopulate(ICommunication communication)
        {
            foreach (Recording recording in Recordings.ToArray())
            {
                if (!File.Exists(recording.FilePath))
                {
                    communication.SendWarningMessage($"TTTAS Recording {recording.Name} not found at path \"{recording.FilePath}\"");
                }
                else
                {
                    //Only add to lookups if it exists
                    RecordingLookup.Add(recording.Name.ToLowerInvariant(), recording);
                }
            }
        }

        public void AddRecording(Recording recording)
        {
            Recordings.Add(recording);
            RecordingLookup.Add(recording.Name.ToLowerInvariant(), recording);
        }
    }

    private record Recording(string Name, string FilePath);

    public class PendingRecording
    {
        public string Name { get; init; }
        public string FilePath { get; init; }
        public string RequestId { get; init; }

        private readonly TaskCompletionSource<bool> requestReadyTrigger = new TaskCompletionSource<bool>();

        public PendingRecording(
            string name,
            string filePath,
            string requestId)
        {
            Name = name;
            FilePath = filePath;
            RequestId = requestId;
        }

        public Task<bool> WaitForReadyAsync() => requestReadyTrigger.Task;
        public void MarkReady() => requestReadyTrigger.TrySetResult(true);
        public void AbortRecording() => requestReadyTrigger.TrySetResult(false);
    }
}
