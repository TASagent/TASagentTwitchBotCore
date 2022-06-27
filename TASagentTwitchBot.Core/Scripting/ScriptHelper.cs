using BGC.Scripting.Parsing;

namespace TASagentTwitchBot.Core.Scripting;

[AutoRegister]
public interface IScriptHelper
{
    [ScriptingAccess]
    ScriptingUser? GetUserByTwitchLogin(string twitchLogin);

    [ScriptingAccess]
    ScriptingUser? GetUserByTwitchId(string twitchId);

    [ScriptingAccess]
    List<ScriptingUser> GetAllUsersWithDatum(string key);

    [ScriptingAccess]
    bool HasGlobalDatum(string key);
    [ScriptingAccess]
    T? GetGlobalDatum<T>(string key);
    [ScriptingAccess]
    void SetGlobalDatum<T>(string key, T value);

    Task<ScriptingUser?> GetUserByTwitchLoginAsync(string twitchLogin);
    Task<ScriptingUser?> GetUserByTwitchIdAsync(string twitchId);
    IAsyncEnumerable<ScriptingUser> GetAllUsersWithDatumAsync(string key);

    ScriptingUser GetScriptingUser(Database.User user);
}

public class ScriptHelper : IScriptHelper
{
    private readonly IPersistentDataManager persistentDataManager;
    private readonly Database.IUserHelper userHelper;

    public ScriptHelper(
        IPersistentDataManager persistentDataManager,
        Database.IUserHelper userHelper)
    {
        this.persistentDataManager = persistentDataManager;
        this.userHelper = userHelper;
    }

    public ScriptingUser? GetUserByTwitchLogin(string twitchLogin)
    {
        Task<ScriptingUser?> task = GetUserByTwitchLoginAsync(twitchLogin);
        task.Wait();

        return task.Result;
    }

    public ScriptingUser? GetUserByTwitchId(string twitchId)
    {
        Task<ScriptingUser?> task = GetUserByTwitchIdAsync(twitchId);
        task.Wait();

        return task.Result;
    }

    public List<ScriptingUser> GetAllUsersWithDatum(string key)
    {
        Task<List<ScriptingUser>> task = CollectUsersWithDatumAsync(key);
        task.Wait();

        return task.Result;
    }

    private async Task<List<ScriptingUser>> CollectUsersWithDatumAsync(string key)
    {
        List<ScriptingUser> userList = new List<ScriptingUser>();
        await foreach(ScriptingUser scriptingUser in GetAllUsersWithDatumAsync(key))
        {
            userList.Add(scriptingUser);
        }

        return userList;
    }

    public async IAsyncEnumerable<ScriptingUser> GetAllUsersWithDatumAsync(string key)
    {
        foreach (string id in persistentDataManager.GetAllUserIdsWithDatum(key))
        {
            yield return (await GetUserByTwitchIdAsync(id))!;
        }
    }

    public async Task<ScriptingUser?> GetUserByTwitchLoginAsync(string twitchLogin)
    {
        Database.User? user = await userHelper.GetUserByTwitchLogin(twitchLogin, false);

        if (user is null)
        {
            return null;
        }

        return GetScriptingUser(user);
    }

    public async Task<ScriptingUser?> GetUserByTwitchIdAsync(string twitchId)
    {
        Database.User? user = await userHelper.GetUserByTwitchId(twitchId, false);

        if (user is null)
        {
            return null;
        }

        return GetScriptingUser(user);
    }

    public bool HasGlobalDatum(string key) => persistentDataManager.HasGlobalDatum(key);

    public T? GetGlobalDatum<T>(string key) => persistentDataManager.GetGlobalDatum<T>(key);

    public void SetGlobalDatum<T>(string key, T value) => SetGlobalDatum(key, value);

    public ScriptingUser GetScriptingUser(Database.User user)
    {
        return new ScriptingUser(
            twitchUserName: user.TwitchUserName,
            twitchUserId: user.TwitchUserId,
            color: string.IsNullOrWhiteSpace(user.Color) ? "#0000FF" : user.Color,
            authorizationLevel: user.AuthorizationLevel,
            ttsVoice: user.TTSVoicePreference,
            ttsPitch: user.TTSPitchPreference,
            ttsSpeed: user.TTSSpeedPreference,
            ttsEffect: user.TTSEffectsChain ?? "",
            persistentDataManager: persistentDataManager);
    }
}