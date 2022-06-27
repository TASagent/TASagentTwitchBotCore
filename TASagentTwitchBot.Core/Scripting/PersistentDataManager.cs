using System.Text.Json;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.Scripting;

[AutoRegister]
public interface IPersistentDataManager
{
    bool HasGlobalDatum(string key);
    bool HasUserDatum(string userId, string key);

    T? GetGlobalDatum<T>(string key);
    T? GetUserDatum<T>(string userId, string key);

    void SetGlobalDatum<T>(string key, T value);
    void SetUserDatum<T>(string userId, string key, T value);

    IEnumerable<string> GetAllUserIdsWithDatum(string key);
}


public class PersistentDataManager : IPersistentDataManager
{
    private readonly PersistentData persistentData;

    public PersistentDataManager()
    {
        persistentData = PersistentData.GetData();
    }

    public bool HasGlobalDatum(string key) => persistentData.HasGlobalDatum(key);
    public bool HasUserDatum(string userId, string key) => persistentData.HasUserDatum(userId, key);

    public T? GetGlobalDatum<T>(string key) => persistentData.GetGlobalDatum<T>(key);
    public T? GetUserDatum<T>(string userId, string key) => persistentData.GetUserDatum<T>(userId, key);

    public void SetGlobalDatum<T>(string key, T value) => persistentData.SetGlobalDatum(key, value);
    public void SetUserDatum<T>(string userId, string key, T value) => persistentData.SetUserDatum(userId, key, value);

    public IEnumerable<string> GetAllUserIdsWithDatum(string key) => persistentData.GetAllUserIdsWithDatum(key);

    private class PersistentData
    {
        private static string FilePath => BGC.IO.DataManagement.PathForDataFile("Data", "PersistentData.json");
        private static readonly object _lock = new object();

        public DataCollection GlobalData { get; init; } = new DataCollection();
        public Dictionary<string, DataCollection> UserData { get; init; } = new Dictionary<string, DataCollection>();

        public static PersistentData GetData()
        {
            PersistentData data;
            if (File.Exists(FilePath))
            {
                data = JsonSerializer.Deserialize<PersistentData>(File.ReadAllText(FilePath))!;
            }
            else
            {
                data = new PersistentData();
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

        public bool HasGlobalDatum(string key) => GlobalData.HasDatum(key);
        public bool HasUserDatum(string userId, string key) => GetUser(userId).HasDatum(key);

        public T? GetGlobalDatum<T>(string key) => GlobalData.GetDatum<T>(key);
        public T? GetUserDatum<T>(string userId, string key)
        {
            if (!UserData.TryGetValue(userId, out DataCollection? userData))
            {
                return default;
            }

            return userData.GetDatum<T>(key);
        }

        public void SetGlobalDatum<T>(string key, T value)
        {
            GlobalData.SetDatum(key, value);
            Serialize();
        }

        public void SetUserDatum<T>(string userId, string key, T value)
        {
            GetUser(userId).SetDatum(key, value);
            Serialize();
        }

        public IEnumerable<string> GetAllUserIdsWithDatum(string key) => UserData.Where(x => x.Value.HasDatum(key)).Select(x => x.Key);

        public DataCollection GetUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"Cannot access null or empty userId", nameof(userId));
            }

            if (!UserData.TryGetValue(userId, out DataCollection? userData))
            {
                userData = new DataCollection();
                UserData[userId] = userData;
            }

            return userData;
        }

        public class DataCollection
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> Data { get; init; } = new Dictionary<string, JsonElement>();

            public bool HasDatum(string key) => Data.ContainsKey(key);

            public T? GetDatum<T>(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException($"Cannot access null or empty data value", nameof(key));
                }

                if (!Data.TryGetValue(key, out JsonElement serializedValue))
                {
                    return default;
                }

                return serializedValue.Deserialize<T>();
            }

            public void SetDatum<T>(string key, T value)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException($"Cannot access null or empty data value", nameof(key));
                }

                Data[key] = JsonSerializer.SerializeToElement(value);
            }
        }
    }

}
