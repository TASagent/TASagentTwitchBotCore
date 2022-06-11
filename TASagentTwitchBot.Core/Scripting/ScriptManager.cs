using BGC.Scripting;
using BGC.Scripting.Parsing;

namespace TASagentTwitchBot.Core.Scripting;

public interface IScriptManager
{
    IEnumerable<string> GetScriptNames();
    string? GetScript(string scriptName);
    string? GetDefaultScript(string scriptName);
    bool SetScript(string scriptName, string script);
}

public interface IScriptRegistrar
{
    GlobalRuntimeContext GlobalSharedRuntimeContext { get; }

    /// <summary>
    /// Register a script that was created after startup
    /// </summary>
    void RegisterNewScript(IScriptedComponent scriptedComponent, string scriptName);

    /// <summary>
    /// Remove a registered script. Returns success.
    /// </summary>
    bool UnregisterScript(string scriptName);
}


public class ScriptManager : IScriptManager, IScriptRegistrar
{
    private readonly ICommunication communication;
    private readonly IScriptHelper scriptHelper;
    private readonly List<IScriptedComponent> scriptedComponents;

    private readonly Dictionary<string, IScriptedComponent> scriptMap = new Dictionary<string, IScriptedComponent>();

    public GlobalRuntimeContext GlobalSharedRuntimeContext { get; } = new GlobalRuntimeContext();

    public ScriptManager(
        ICommunication communication,
        IScriptHelper scriptHelper,
        IEnumerable<IScriptedComponent> scriptedComponents)
    {
        this.communication = communication;
        this.scriptHelper = scriptHelper;
        this.scriptedComponents = scriptedComponents.ToList();

        Initialize();
    }

    private void Initialize()
    {
        ClassRegistrar.TryRegisterClass<ICommunication>("ICommunication", limited: true);
        GlobalSharedRuntimeContext.AddOrSetValue("communication", typeof(ICommunication), communication);

        ClassRegistrar.TryRegisterClass<IScriptHelper>("IScriptHelper", limited: true);
        GlobalSharedRuntimeContext.AddOrSetValue("scriptHelper", typeof(IScriptHelper), scriptHelper);

        foreach (IScriptedComponent scriptedComponent in scriptedComponents)
        {
            scriptedComponent.Initialize(this);

            foreach (string script in scriptedComponent.GetScriptNames())
            {
                scriptMap.Add(script, scriptedComponent);
            }
        }
    }

    public IEnumerable<string> GetScriptNames() => scriptMap.Keys;

    public string? GetDefaultScript(string scriptName)
    {
        if (!scriptMap.TryGetValue(scriptName, out IScriptedComponent? scriptedComponent))
        {
            communication.SendWarningMessage($"Requested unregistered script name {scriptName}");
            return null;
        }

        return scriptedComponent.GetDefaultScript(scriptName);
    }

    public string? GetScript(string scriptName)
    {
        if (!scriptMap.TryGetValue(scriptName, out IScriptedComponent? scriptedComponent))
        {
            communication.SendWarningMessage($"Requested unregistered script name {scriptName}");
            return null;
        }

        return scriptedComponent.GetScript(scriptName);
    }

    public bool SetScript(string scriptName, string script)
    {
        if (!scriptMap.TryGetValue(scriptName, out IScriptedComponent? scriptedComponent))
        {
            communication.SendWarningMessage($"Requested unregistered script name {scriptName}");
            return false;
        }

        return scriptedComponent.SetScript(scriptName, script);
    }

    public void RegisterNewScript(IScriptedComponent scriptedComponent, string scriptName)
    {
        if (!scriptedComponents.Contains(scriptedComponent))
        {
            communication.SendErrorMessage($"Called RegisterNewScript on a IScriptedComponent that was not already registered. " +
                $"For the time being this is supported, but it should probably be registered in Program.cs with AddTASSingleton: {scriptedComponent}");

            scriptedComponents.Add(scriptedComponent);
        }

        scriptMap.Add(scriptName, scriptedComponent);
    }

    public bool UnregisterScript(string scriptName) => scriptMap.Remove(scriptName);
}
