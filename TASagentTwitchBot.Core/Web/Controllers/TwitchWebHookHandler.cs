using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;
using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [Route("/TASagentBotAPI/WebSub/[Action]")]
    [ConditionalFeature("Notifications")]
    public class TwitchWebHookController : ControllerBase
    {
        public TwitchWebHookController() { }

        [HttpPost]
        [WebSubMethod]
        public async Task<IActionResult> Followers(
            [FromServices] WebSub.IFollowSubscriber followSubscriber,
            TwitchFollowData _)
        {
            //I am unclear on why, but I have to deserialize the follow from the body manually
            Request.Body.Position = 0;
            using StreamReader reader = new StreamReader(
                Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: -1,
                leaveOpen: true);

            string body = reader.ReadToEnd();

            TwitchFollowData follow = JsonSerializer.Deserialize<TwitchFollowData>(body);

            foreach (var datum in follow.Data)
            {
                await followSubscriber.NotifyFollower(datum.FromID, datum.FromName);
            }

            return Ok();
        }

        [HttpPost]
        [WebSubMethod]
        public IActionResult Stream(
            [FromServices] WebSub.IStreamChangeSubscriber streamChangeSubscriber,
            TwitchStreams _)
        {
            //I am unclear on why, but I have to deserialize the follow from the body manually
            Request.Body.Position = 0;
            using StreamReader reader = new StreamReader(
                Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: -1,
                leaveOpen: true);

            string body = reader.ReadToEnd();

            TwitchStreams channelData = JsonSerializer.Deserialize<TwitchStreams>(body);

            if (channelData.Data is not null && channelData.Data.Count > 0)
            {
                streamChangeSubscriber.NotifyUpdate(channelData.Data[0]);
            }
            else
            {
                streamChangeSubscriber.NotifyUpdate(null);
            }

            return Ok();
        }

        public record TwitchFollowData(
            [property: JsonPropertyName("data")] List<TwitchFollows.Datum> Data)
        {
            public record Datum(
                [property: JsonPropertyName("from_id")] string FromID,
                [property: JsonPropertyName("from_name")] string FromName,
                [property: JsonPropertyName("to_id")] string ToID,
                [property: JsonPropertyName("to_name")] string ToName,
                [property: JsonPropertyName("followed_at")] string FollowedAt);
        }
    }
}
