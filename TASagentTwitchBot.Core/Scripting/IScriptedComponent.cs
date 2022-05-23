namespace TASagentTwitchBot.Core.Scripting;

public interface IScriptedComponent
{
    void Initialize(IScriptRegistrar scriptRegistrar);

    IEnumerable<string> GetScriptNames();
    string? GetScript(string scriptName);
    string? GetDefaultScript(string scriptName);
    bool SetScript(string scriptName, string script);
}
