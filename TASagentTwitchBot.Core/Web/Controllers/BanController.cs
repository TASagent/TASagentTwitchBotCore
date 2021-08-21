using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TASagentTwitchBot.Core.Chat;
using TASagentTwitchBot.Core.Web.Middleware;
namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Ban/[action]")]
    [ConditionalFeature("Database")]
    public class BanController : ControllerBase
    {


        private readonly IBanHandler banHandler;

        public BanController(IBanHandler banHandler)
        {
            this.banHandler = banHandler;
        }


        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public async Task<BanHelpers.BanRuleDTO> AddBanRule(BanHelpers.BanRuleDTO banRule)
        {
            var bR = await banHandler.AddBanRule(banRule);
            return bR;
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public async Task<IActionResult> RemoveBanRule(BanHelpers.BanRuleDTO banRule)
        {
            await banHandler.RemoveBanRule(banRule);
            return Ok();
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<IEnumerable<BanHelpers.BanRuleDTO>> GetBanRules()
        {
            return banHandler.GetBanRules()
                .Select(x => BanHelpers.RuleToDTO(x))
                .ToList();
        }


    }
}

