﻿using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.WebServer.TTS;
using TASagentTwitchBot.Core.WebServer.Web;

namespace TASagentTwitchBot.Core.WebServer.Controllers;

[ApiController]
[Route("/TASagentServerAPI/DataForwarding/{userNameString}/{context}")]
public class DataForwardingController : Controller
{
    private readonly IServerDataForwardingHandler dataForwardingHandler;

    public DataForwardingController(
        IServerDataForwardingHandler dataForwardingHandler)
    {
        this.dataForwardingHandler = dataForwardingHandler;
    }

    [HttpGet("")]
    [AllowCrossSite]
    public ActionResult<List<ServerDataFile>> List(string userNameString, string context)
    {
        return dataForwardingHandler.GetDataFileList(userNameString, context);
    }

    [HttpGet("{dataFileAlias}")]
    [AllowCrossSite]
    public async Task<IActionResult> Fetch(string userNameString, string context, string dataFileAlias)
    {
        if (string.IsNullOrEmpty(dataFileAlias))
        {
            return BadRequest();
        }

        if (dataFileAlias.StartsWith('/'))
        {
            dataFileAlias = dataFileAlias[1..];
        }

        ServerFileData? fileData = await dataForwardingHandler.GetDataFileByAlias(userNameString, context, dataFileAlias);

        if (fileData is null)
        {
            return NotFound();
        }

        return File(
            fileContents: fileData.Data,
            contentType: fileData.ContentType ?? "",
            fileDownloadName: "");
    }
}
