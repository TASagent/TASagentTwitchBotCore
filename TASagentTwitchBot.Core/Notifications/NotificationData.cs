using System;

namespace TASagentTwitchBot.Core.Notifications
{
    public record NotificationData(string MediaType, string Text, double Duration);
    public record ImageNotificationData(string Image, string Text, double Duration) : NotificationData("image", Text, Duration);
    public record VideoNotificationData(string Video, string Text, double Duration) : NotificationData("video", Text, Duration);
}
