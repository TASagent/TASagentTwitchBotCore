using System.Web;

namespace TASagentTwitchBot.Core.Notifications
{
    public class MarqueeMessage
    {
        public readonly string sender;
        public readonly string senderFontColor;
        public readonly string message;

        public MarqueeMessage(string sender, string message, string senderFontColor = null)
        {
            this.sender = sender;
            this.message = message;
            this.senderFontColor = senderFontColor;

            if (string.IsNullOrWhiteSpace(senderFontColor))
            {
                this.senderFontColor = "#0000FF";
            }
        }

        public string GetMessage() =>
            $"<h1><span style=\"color: {senderFontColor}\">{HttpUtility.HtmlEncode(sender)}</span>: {HttpUtility.HtmlEncode(message)}</h1>";
    }

}
