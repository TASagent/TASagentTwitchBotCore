using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Timer/[action]")]
    [ConditionalFeature("Overlay")]
    public class TimerController : Controller
    {
        private readonly Timer.ITimerManager timerManager;

        public TimerController(
            Timer.ITimerManager timerManager)
        {
            this.timerManager = timerManager;
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Start()
        {
            timerManager.Start();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Stop()
        {
            timerManager.Stop();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Reset()
        {
            timerManager.Reset();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Set(TimeValue timeValue)
        {
            timerManager.Set(timeValue.Time);
            return Ok();
        }


        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult MarkLap()
        {
            timerManager.MarkLap();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult UnmarkLap()
        {
            timerManager.UnmarkLap();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult ResetCurrentLap()
        {
            timerManager.ResetCurrentLap();
            return Ok();
        }


        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult MarkLapAtAbsolute(TimeValue timeValue)
        {
            timerManager.MarkLapAtAbsolute(timeValue.Time);
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult MarkLapAtRelative(TimeValue timeValue)
        {
            timerManager.MarkLapAtRelative(timeValue.Time);
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult DropLap(LapIndex lap)
        {
            timerManager.DropLap(lap.Index);
            return Ok();
        }

        [HttpGet]
        public ActionResult<IEnumerable<TimerLayoutValue>> DisplayModes()
        {
            List<TimerLayoutValue> displayModes = new List<TimerLayoutValue>();

            for (Timer.TimerDisplayMode mode = 0; mode < Timer.TimerDisplayMode.MAX; mode++)
            {
                displayModes.Add(new TimerLayoutValue(mode.ToString(), mode));
            }

            return displayModes;
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<Timer.TimerState> TimerState()
        {
            return timerManager.GetTimerState();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult DisplayMode(Timer.TimerLayoutData layout)
        {
            timerManager.UpdateLayout(layout);
            return Ok();
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<IEnumerable<Timer.TimerData>> SavedTimers()
        {
            return timerManager.GetSavedTimers();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public async Task<IActionResult> LoadTimer(TimerIdentifier timer)
        {
            if (string.IsNullOrEmpty(timer.TimerName))
            {
                return BadRequest();
            }

            if (!await timerManager.LoadTimer(timer.TimerName))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult SaveTimer(TimerIdentifier timer)
        {
            if (string.IsNullOrEmpty(timer.TimerName))
            {
                return BadRequest();
            }

            timerManager.SaveTimer(timer.TimerName);
            return Ok();
        }

        public record TimerLayoutValue(string Display, Timer.TimerDisplayMode Value);
        public record TimeValue(double Time);
        public record LapIndex(int Index);
        public record TimerIdentifier(string TimerName);
        public record TimerLayout(string MainLabel, string MainDisplay, string SecondaryLabel, string SecondaryDisplay);
    }
}
