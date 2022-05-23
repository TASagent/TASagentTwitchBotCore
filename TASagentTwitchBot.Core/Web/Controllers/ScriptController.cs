using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Scripting;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Scripting/[action]")]
[ConditionalFeature("Scripting")]
public class ScriptController : ControllerBase
{
    private readonly IScriptManager scriptManager;

    public ScriptController(
        IScriptManager scriptManager)
    {
        this.scriptManager = scriptManager;
    }

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<List<string>> Scripts() =>
        scriptManager.GetScriptNames().ToList();

    [HttpGet("{scriptName}")]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string> Script(string scriptName)
    {
        string? script = scriptManager.GetScript(scriptName);

        if (script == null)
        {
            return BadRequest();
        }

        return script;
    }

    [HttpGet("{scriptName}")]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string> DefaultScript(string scriptName)
    {
        string? script = scriptManager.GetDefaultScript(scriptName);

        if (script == null)
        {
            return BadRequest();
        }

        return script;
    }

    [HttpPost("{scriptName}")]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult Script(string scriptName, ScriptUpdate scriptUpdate)
    {
        if (!scriptManager.SetScript(scriptName, scriptUpdate.Script))
        {
            return BadRequest();
        }

        return Ok();
    }

    public record ScriptUpdate(string Script);
}
