using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BGC.Audio;
using BGC.Audio.NAudio;

namespace TASagentTwitchBot.Core.Audio
{
    public interface ISoundEffectSystem
    {
        bool HasSoundEffects();
        SoundEffect GetSoundEffectByAlias(string alias);
        SoundEffect GetSoundEffectByName(string name);

        List<string> GetSoundEffects();

        ReverbIRF GetReverbEffectByAlias(string alias);
        ReverbIRF GetReverbEffectByName(string name);

        List<string> GetReverbEffects();
    }


    public class SoundEffectSystem : ISoundEffectSystem
    {
        private readonly ICommunication communication;

        private SoundEffectData soundEffectData;
        private readonly string dataFilePath;


        public SoundEffectSystem(
            ICommunication communication)
        {
            this.communication = communication;

            dataFilePath = BGC.IO.DataManagement.PathForDataFile("Config", "SoundEffects.json");

            if (File.Exists(dataFilePath))
            {
                soundEffectData = JsonSerializer.Deserialize<SoundEffectData>(File.ReadAllText(dataFilePath));
                soundEffectData.VerifyAndPopulate(communication);
            }
            else
            {
                soundEffectData = new SoundEffectData();
                File.WriteAllText(dataFilePath, JsonSerializer.Serialize(soundEffectData));
            }
        }

        public bool HasSoundEffects() => soundEffectData.SoundEffects.Count != 0;

        public List<string> GetSoundEffects() => new List<string>(soundEffectData.SoundEffects.Select(x => x.Name));

        public SoundEffect GetSoundEffectByAlias(string alias)
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

            if (!soundEffectData.SoundEffectAliasLookup.ContainsKey(alias))
            {
                return null;
            }

            return soundEffectData.SoundEffectAliasLookup[alias];
        }

        public SoundEffect GetSoundEffectByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            name = name.ToLowerInvariant();

            if (!soundEffectData.SoundEffectNameLookup.ContainsKey(name))
            {
                return null;
            }

            return soundEffectData.SoundEffectNameLookup[name];
        }

        public List<string> GetReverbEffects() => new List<string>(soundEffectData.ReverbIRFs.Select(x => x.Name));

        public ReverbIRF GetReverbEffectByAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return null;
            }

            alias = alias.ToLowerInvariant();

            if (!soundEffectData.ReverbIRFAliasLookup.ContainsKey(alias))
            {
                return null;
            }

            return soundEffectData.ReverbIRFAliasLookup[alias];
        }

        public ReverbIRF GetReverbEffectByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            name = name.ToLowerInvariant();

            if (!soundEffectData.ReverbIRFNameLookup.ContainsKey(name))
            {
                return null;
            }

            return soundEffectData.ReverbIRFNameLookup[name];
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

            if (newAliases.Count == 0)
            {
                communication.SendErrorMessage($"Sound effect \"{name}\" has no remaining unique aliases");
                return;
            }

            SoundEffect newSoundEffect = new SoundEffect(name, filePath, newAliases.ToArray());

            soundEffectData.SoundEffects.Add(newSoundEffect);
            soundEffectData.SoundEffectNameLookup.Add(newSoundEffect.Name.ToLowerInvariant(), newSoundEffect);

            foreach (string alias in newAliases)
            {
                soundEffectData.SoundEffectAliasLookup.Add(alias.ToLowerInvariant(), newSoundEffect);
            }

            if (serialize)
            {
                Serialize();
            }
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
                Serialize();
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

        private void Serialize()
        {
            lock (soundEffectData)
            {
                File.WriteAllText(dataFilePath, JsonSerializer.Serialize(soundEffectData));
            }
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
                stream: out IBGCStream filter);

            if (!loadSuccess)
            {
                return null;
            }

            return filter;
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

            public void VerifyAndPopulate(ICommunication communication)
            {
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

    public record SoundEffect(string Name, string FilePath, string[] Aliases);
    public record ReverbIRF(string Name, string FilePath, double Gain, string[] Aliases);
}
