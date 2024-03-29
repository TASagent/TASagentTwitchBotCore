﻿using System.Text.Json;
using System.Text.Json.Serialization;
using BGC.Audio;
using BGC.Audio.NAudio;

namespace TASagentTwitchBot.Core.Audio;

[AutoRegister]
public interface ISoundEffectSystem
{
    bool HasSoundEffects();
    SoundEffect? GetSoundEffectByAlias(string alias);
    SoundEffect? GetSoundEffectByName(string name);

    SoundEffect? GetAnySoundEffect();

    void AddEffect(string name, string filePath, string[] aliases, bool serialize);
    bool RemoveEffect(string name);

    IReadOnlyList<SoundEffect> GetAllSoundEffects();
    IEnumerable<string> GetAllSoundEffectNames();
    IEnumerable<SoundEffect> GetSoundEffects(int page, int count);

    ReverbIRF? GetReverbEffectByAlias(string alias);
    ReverbIRF? GetReverbEffectByName(string name);

    List<string> GetReverbEffects();
}


public class SoundEffectSystem : ISoundEffectSystem
{
    private readonly ICommunication communication;

    private readonly SoundEffectData soundEffectData;


    public SoundEffectSystem(
        ICommunication communication)
    {
        this.communication = communication;

        soundEffectData = SoundEffectData.GetData(communication);
    }

    public bool HasSoundEffects() => soundEffectData.SoundEffects.Count != 0;

    public IEnumerable<string> GetAllSoundEffectNames() => soundEffectData.SoundEffects.Select(x => x.Name);
    public IEnumerable<SoundEffect> GetSoundEffects(int page, int count) => soundEffectData.SoundEffects.Skip(page * count).Take(count);

    public SoundEffect? GetAnySoundEffect() => soundEffectData.SoundEffects.FirstOrDefault();

    public IReadOnlyList<SoundEffect> GetAllSoundEffects() => soundEffectData.SoundEffects;

    public SoundEffect? GetSoundEffectByAlias(string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return null;
        }

        alias = alias.ToLowerInvariant();

        if (alias.StartsWith('/'))
        {
            alias = alias[1..];
        }

        return soundEffectData.SoundEffectAliasLookup.GetValueOrDefault(alias);
    }

    public SoundEffect? GetSoundEffectByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return soundEffectData.SoundEffectNameLookup.GetValueOrDefault(name.ToLowerInvariant());
    }

    public List<string> GetReverbEffects() => new List<string>(soundEffectData.ReverbIRFs.Select(x => x.Name));

    public ReverbIRF? GetReverbEffectByAlias(string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return null;
        }

        return soundEffectData.ReverbIRFAliasLookup.GetValueOrDefault(alias.ToLowerInvariant());
    }

    public ReverbIRF? GetReverbEffectByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return soundEffectData.ReverbIRFNameLookup.GetValueOrDefault(name.ToLowerInvariant());
    }

    public void AddEffect(string name, string filePath, string alias, bool serialize = true) =>
        AddEffect(name, filePath, new[] { alias }, serialize);

    public void AddEffect(string name, string filePath, string[] aliases, bool serialize = true)
    {
        if (!File.Exists(filePath))
        {
            communication.SendWarningMessage($"Sound effect \"{name}\" not found at path \"{filePath}\"");
            return;
        }

        List<string> newAliases;

        if (soundEffectData.SoundEffects.FirstOrDefault(x => x.FilePath == filePath) is SoundEffect existingSoundEffect)
        {
            //Expanding existing definition
            communication.SendWarningMessage($"Sound effect submitted for \"{name}\" already exists.  Expanding aliases under name \"{existingSoundEffect.Name}\"");

            //Update Name
            name = existingSoundEffect.Name;

            //Get Unique set of aliases
            newAliases = new List<string>(aliases.Union(existingSoundEffect.Aliases));

            //Remove existing links to old sound effect
            soundEffectData.SoundEffects.Remove(existingSoundEffect);
            soundEffectData.SoundEffectNameLookup.Remove(existingSoundEffect.Name.ToLowerInvariant());
            foreach (string oldAlias in existingSoundEffect.Aliases)
            {
                soundEffectData.SoundEffectAliasLookup.Remove(oldAlias.ToLowerInvariant());
            }
        }
        else
        {
            //New definition
            newAliases = new List<string>(aliases);
        }

        for (int i = newAliases.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(newAliases[i]))
            {
                //Bad Alias
                communication.SendErrorMessage($"Sound effect \"{name}\" submitted with a null alias");
                //Skip this alias
                newAliases.RemoveAt(i);
                continue;
            }

            newAliases[i] = newAliases[i].ToLowerInvariant();

            if (soundEffectData.SoundEffectAliasLookup.ContainsKey(newAliases[i]))
            {
                //Already added
                communication.SendWarningMessage($"Sound effect alias \"{newAliases[i]}\" for \"{name}\" already defined. " +
                    $"Old: \"{soundEffectData.SoundEffectAliasLookup[newAliases[i]].FilePath}\"  New: \"{filePath}\"");
                //Skip this alias
                newAliases.RemoveAt(i);
                continue;
            }
        }

        SoundEffect newSoundEffect = new SoundEffect(name, filePath, newAliases.ToArray());

        soundEffectData.SoundEffects.Add(newSoundEffect);
        soundEffectData.SoundEffectNameLookup.Add(newSoundEffect.Name.ToLowerInvariant(), newSoundEffect);

        //Keep sound effects sorted
        soundEffectData.SoundEffects.Sort();

        foreach (string alias in newAliases)
        {
            soundEffectData.SoundEffectAliasLookup.Add(alias.ToLowerInvariant(), newSoundEffect);
        }

        communication.SendDebugMessage($"Sound effect \"{name}\" added.");

        if (serialize)
        {
            soundEffectData.Serialize();
        }
    }

    public bool RemoveEffect(string name)
    {
        SoundEffect? soundEffect = GetSoundEffectByName(name.ToLowerInvariant());

        if (soundEffect is null)
        {
            return false;
        }

        soundEffectData.SoundEffects.Remove(soundEffect);
        soundEffectData.SoundEffectNameLookup.Remove(soundEffect.Name.ToLowerInvariant());

        foreach (string alias in soundEffect.Aliases)
        {
            soundEffectData.SoundEffectAliasLookup.Remove(alias.ToLowerInvariant());
        }

        soundEffectData.Serialize();

        return true;
    }

    public void AddReverbIRF(string name, string filePath, string[] aliases, bool serialize = true)
    {
        if (!File.Exists(filePath))
        {
            communication.SendWarningMessage($"Reverb IRF \"{name}\" not found at path \"{filePath}\"");
            return;
        }

        List<string> newAliases;

        if (soundEffectData.ReverbIRFs.FirstOrDefault(x => x.FilePath == filePath) is ReverbIRF existingIRF)
        {
            //Expanding existing definition
            communication.SendWarningMessage($"Reverb IRF submitted for \"{name}\" already exists.  Expanding aliases under name \"{existingIRF.Name}\"");

            //Update Name
            name = existingIRF.Name;

            //Get Unique set of aliases
            newAliases = new List<string>(aliases.Union(existingIRF.Aliases));

            //Remove existing links to old IRFs
            soundEffectData.ReverbIRFs.Remove(existingIRF);
            soundEffectData.ReverbIRFNameLookup.Remove(existingIRF.Name.ToLowerInvariant());
            foreach (string oldAlias in existingIRF.Aliases)
            {
                soundEffectData.ReverbIRFAliasLookup.Remove(oldAlias.ToLowerInvariant());
            }
        }
        else
        {
            //New definition
            newAliases = new List<string>(aliases);
        }

        for (int i = newAliases.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(newAliases[i]))
            {
                //Bad Alias
                communication.SendErrorMessage($"Reverb IRF \"{name}\" submitted with a null alias");
                //Skip this alias
                newAliases.RemoveAt(i);
                continue;
            }

            newAliases[i] = newAliases[i].ToLowerInvariant();

            if (soundEffectData.ReverbIRFAliasLookup.ContainsKey(newAliases[i]))
            {
                //Already added
                communication.SendWarningMessage($"Reverb IRF alias \"{newAliases[i]}\" for \"{name}\" already defined. " +
                    $"Old: \"{soundEffectData.ReverbIRFAliasLookup[newAliases[i]].FilePath}\"  New: \"{filePath}\"");
                //Skip this alias
                newAliases.RemoveAt(i);
                continue;
            }
        }

        if (newAliases.Count == 0)
        {
            communication.SendErrorMessage($"Reverb IRF \"{name}\" has no remaining unique aliases");
            return;
        }

        if (string.IsNullOrEmpty(soundEffectData.ReverbTestFile))
        {
            if (soundEffectData.SoundEffects.Count == 0)
            {
                communication.SendErrorMessage($"Reverb file cannot be added - No Test File defined and no sound effects added.");
                return;
            }

            soundEffectData.ReverbTestFile = soundEffectData.SoundEffects[0].FilePath;
            communication.SendWarningMessage($"Reverb test file not specified - using first sound effect \"{soundEffectData.ReverbTestFile}\"");
        }

        //Calculate Gain
        double gain = CalculateFilterGain(filePath);

        if (double.IsNaN(gain))
        {
            communication.SendErrorMessage($"Failed to calculate filter \"{name}\" gain for file \"{filePath}\".");
            return;
        }

        communication.SendDebugMessage($"Calculated filter \"{name}\" gain to be {gain}.");

        ReverbIRF newReverbIRF = new ReverbIRF(name, filePath, gain, newAliases.ToArray());

        soundEffectData.ReverbIRFs.Add(newReverbIRF);
        soundEffectData.ReverbIRFNameLookup.Add(newReverbIRF.Name.ToLowerInvariant(), newReverbIRF);

        foreach (string alias in newAliases)
        {
            soundEffectData.ReverbIRFAliasLookup.Add(alias.ToLowerInvariant(), newReverbIRF);
        }

        if (serialize)
        {
            soundEffectData.Serialize();
        }
    }

    private double CalculateFilterGain(string filterPath)
    {
        using DisposableWaveProvider audioFile = AudioTools.GetWaveProvider(soundEffectData.ReverbTestFile);
        using DisposableWaveProvider irFileReader = AudioTools.GetWaveProvider(filterPath);

        IBGCStream filter = irFileReader.ToBGCStream().SafeCache();
        IBGCStream audioStream = audioFile.ToBGCStream().EnsureMono().SafeCache();

        double initialFileRMS = audioStream.CalculateRMS().First();

        IBGCStream convolvedStreamA = audioStream.MultiConvolve(filter);
        double finalAudioFileRMS = convolvedStreamA.CalculateRMS().Max();

        return finalAudioFileRMS / initialFileRMS;
    }

    public static IBGCStream Spatialize(
        IBGCStream stream,
        double angle)
    {
        return new BGC.Audio.Filters.MultiConvolutionFilter(stream, GetSpatialFilter(angle));
    }

    private static IBGCStream GetSpatialFilter(double angle)
    {
        int position = NearestValidOffset(angle);

        string path = GetSpatialFilterFilename(position);

        bool loadSuccess = WaveEncoding.LoadBGCStream(
            filepath: path,
            stream: out IBGCStream? filter);

        if (!loadSuccess)
        {
            throw new Exception($"Unable to load Spatial Filter for angle: {angle}");
        }

        return filter!;
    }

    private static int NearestValidOffset(double offset)
    {
        if (offset < -90.0 || offset > 90.0)
        {
            offset = BGC.Mathematics.GeneralMath.Clamp(offset, -90.0, 90.0);
        }

        return (int)Math.Round(10.0 * offset);
    }

    private static string GetSpatialFilterFilename(int position)
    {
        string filePrefix;

        if (position == 0)
        {
            filePrefix = "0";
        }
        else
        {
            string directionPrefix = (position > 0) ? "pos" : "neg";

            position = Math.Abs(position);

            int decimalPlace = position % 10;
            int integralPlace = position / 10;

            filePrefix = $"{directionPrefix}{integralPlace}p{decimalPlace}";
        }

        string filterFilesPath = BGC.IO.DataManagement.PathForDataDirectory("Filters");
        return Path.Combine(filterFilesPath, "HRTF", $"{filePrefix}_impulse.wav");
    }

    public class SoundEffectData
    {
        private static string FilePath => BGC.IO.DataManagement.PathForDataFile("Config", "SoundEffects.json");
        private static readonly object _lock = new object();

        public string ReverbTestFile { get; set; } = "";

        public List<SoundEffect> SoundEffects { get; init; } = new List<SoundEffect>();
        public List<ReverbIRF> ReverbIRFs { get; init; } = new List<ReverbIRF>();

        [JsonIgnore]
        public Dictionary<string, SoundEffect> SoundEffectNameLookup { get; } = new Dictionary<string, SoundEffect>();
        [JsonIgnore]
        public Dictionary<string, ReverbIRF> ReverbIRFNameLookup { get; } = new Dictionary<string, ReverbIRF>();

        [JsonIgnore]
        public Dictionary<string, SoundEffect> SoundEffectAliasLookup { get; } = new Dictionary<string, SoundEffect>();
        [JsonIgnore]
        public Dictionary<string, ReverbIRF> ReverbIRFAliasLookup { get; } = new Dictionary<string, ReverbIRF>();

        public static SoundEffectData GetData(ICommunication communication)
        {
            SoundEffectData data;
            if (File.Exists(FilePath))
            {
                data = JsonSerializer.Deserialize<SoundEffectData>(File.ReadAllText(FilePath))!;
                data.VerifyAndPopulate(communication);
            }
            else
            {
                data = new SoundEffectData();
                data.Serialize();
            }

            return data;
        }

        public void Serialize()
        {
            lock (_lock)
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
            }
        }

        private void VerifyAndPopulate(ICommunication communication)
        {
            //Keep SoundEffects Sorted
            SoundEffects.Sort();

            foreach (SoundEffect soundEffect in SoundEffects.ToArray())
            {
                if (!File.Exists(soundEffect.FilePath))
                {
                    communication.SendWarningMessage($"Sound effect {soundEffect.Name} not found at path \"{soundEffect.FilePath}\"");
                }
                else
                {
                    //Only add to lookups if it exists
                    SoundEffectNameLookup.Add(soundEffect.Name.ToLowerInvariant(), soundEffect);

                    foreach (string alias in soundEffect.Aliases)
                    {
                        SoundEffectAliasLookup.Add(alias.ToLowerInvariant(), soundEffect);
                    }
                }
            }

            foreach (ReverbIRF reverbIRF in ReverbIRFs.ToArray())
            {
                if (!File.Exists(reverbIRF.FilePath))
                {
                    communication.SendWarningMessage($"Reverb IRF {reverbIRF.Name} not found at path \"{reverbIRF.FilePath}\"");
                }
                else
                {
                    //Only add to lookups if it exists
                    ReverbIRFNameLookup.Add(reverbIRF.Name.ToLowerInvariant(), reverbIRF);

                    foreach (string alias in reverbIRF.Aliases)
                    {
                        ReverbIRFAliasLookup.Add(alias.ToLowerInvariant(), reverbIRF);
                    }
                }
            }
        }
    }
}

public record SoundEffect(string Name, string FilePath, string[] Aliases) : IComparable
{
    int IComparable.CompareTo(object? obj)
    {
        if (obj is not SoundEffect other)
        {
            return 1;
        }

        return Name.CompareTo(other.Name);
    }
}

public record ReverbIRF(string Name, string FilePath, double Gain, string[] Aliases);
