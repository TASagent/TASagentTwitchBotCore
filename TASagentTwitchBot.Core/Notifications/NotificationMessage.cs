using System;

namespace TASagentTwitchBot.Core.Notifications
{
    public abstract class NotificationMessage
    {
        public abstract NotificationData GetNotificationData();
    }

    public class ImageNotificationMessage : NotificationMessage
    {
        public readonly string image;
        public readonly double duration;
        public readonly string message;

        public ImageNotificationMessage(
            string image,
            double duration,
            string message)
        {
            this.image = image;
            this.duration = duration;
            this.message = message;
        }

        public override NotificationData GetNotificationData()
        {
            return new ImageNotificationData(
                Image: GetImage(),
                Text: GetMessage(),
                Duration: duration);
        }

        public string GetImage()
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return "";
            }

            return $"<img src=\"{image}\">";
        }

        public string GetMessage()
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "";
            }

            return $"<h1>{message}</h1>";
        }
    }

    public class VideoNotificationMessage : NotificationMessage
    {
        public readonly string videoURL;
        public readonly string message;
        public readonly double duration;

        public VideoNotificationMessage(
            string videoURL,
            string videoFilePath,
            string message)
        {
            this.videoURL = videoURL;
            this.message = message;

            using NAudio.Wave.MediaFoundationReader mediaFile = new NAudio.Wave.MediaFoundationReader(videoFilePath);
            duration = mediaFile.TotalTime.TotalMilliseconds;
        }

        public override NotificationData GetNotificationData()
        {
            return new VideoNotificationData(
                Video: GetVideo(),
                Text: GetMessage(),
                Duration: duration);
        }

        public string GetVideo()
        {
            if (string.IsNullOrWhiteSpace(videoURL))
            {
                return "";
            }

            return $"<video src=\"{videoURL}\" type=\"video/mp4\" autoplay muted>";
        }

        public string GetMessage()
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "";
            }

            return $"<h1>{message}</h1>";
        }
    }
}
