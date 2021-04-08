using System;
using System.Collections.Generic;
using System.Text;

namespace TASagentTwitchBot.Core.IRC
{
    /// <summary>
    /// Partially taken from https://github.com/TwitchLib/TwitchLib
    /// </summary>
    public readonly struct IRCMessage
    {
        public readonly string user;
        public readonly string hostmask;
        public readonly string parameter;
        public readonly string message;
        public readonly IRCCommand ircCommand;
        public readonly IReadOnlyDictionary<string, string> tags;

        public IRCMessage(string raw)
        {
            Dictionary<string, string> tagDict = new Dictionary<string, string>();

            ParserState state = ParserState.None;
            int[] starts = new[] { 0, 0, 0, 0, 0, 0 };
            int[] lens = new[] { 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < raw.Length; ++i)
            {
                lens[(int)state] = i - starts[(int)state] - 1;
                if (state == ParserState.None && raw[i] == '@')
                {
                    state = ParserState.V3;
                    starts[(int)state] = ++i;

                    int start = i;
                    string key = null;
                    for (; i < raw.Length; ++i)
                    {
                        if (raw[i] == '=')
                        {
                            key = raw[start..i];
                            start = i + 1;
                        }
                        else if (raw[i] == ';')
                        {
                            if (key == null)
                            {
                                tagDict[raw[start..i]] = "1";
                            }
                            else
                            {
                                tagDict[key] = raw[start..i];
                            }

                            start = i + 1;
                        }
                        else if (raw[i] == ' ')
                        {
                            if (key == null)
                            {
                                tagDict[raw[start..i]] = "1";
                            }
                            else
                            {
                                tagDict[key] = raw[start..i];
                            }

                            break;
                        }
                    }
                }
                else if (state < ParserState.Prefix && raw[i] == ':')
                {
                    state = ParserState.Prefix;
                    starts[(int)state] = ++i;
                }
                else if (state < ParserState.Command)
                {
                    state = ParserState.Command;
                    starts[(int)state] = i;
                }
                else if (state < ParserState.Trailing && raw[i] == ':')
                {
                    state = ParserState.Trailing;
                    starts[(int)state] = ++i;
                    break;
                }
                else if (state < ParserState.Trailing && raw[i] == '+' || state < ParserState.Trailing && raw[i] == '-')
                {
                    state = ParserState.Trailing;
                    starts[(int)state] = i;
                    break;
                }
                else if (state == ParserState.Command)
                {
                    state = ParserState.Param;
                    starts[(int)state] = i;
                }

                while (i < raw.Length && raw[i] != ' ')
                {
                    ++i;
                }
            }

            lens[(int)state] = raw.Length - starts[(int)state];
            string cmd = raw.Substring(starts[(int)ParserState.Command], lens[(int)ParserState.Command]);

            ircCommand = ParseCommand(cmd);

            parameter = raw.Substring(starts[(int)ParserState.Param], lens[(int)ParserState.Param]);
            message = raw.Substring(starts[(int)ParserState.Trailing], lens[(int)ParserState.Trailing]);
            hostmask = raw.Substring(starts[(int)ParserState.Prefix], lens[(int)ParserState.Prefix]);

            if (tagDict.Count > 0)
            {
                tags = tagDict;
            }
            else
            {
                tags = null;
            }

            int idx = hostmask.IndexOf('!');
            user = idx != -1 ? hostmask.Substring(0, idx) : hostmask;
        }

        public IRCMessage(
            string user,
            string hostmask,
            string parameter,
            string message,
            IRCCommand ircCommand,
            IReadOnlyDictionary<string, string> tags)
        {
            this.user = user;
            this.hostmask = hostmask;
            this.parameter = parameter;
            this.message = message;
            this.ircCommand = ircCommand;
            this.tags = tags;
        }

        private static IRCCommand ParseCommand(string cmd)
        {
            switch (cmd)
            {
                case "PRIVMSG": return IRCCommand.PrivMsg;
                case "NOTICE": return IRCCommand.Notice;
                case "PING": return IRCCommand.Ping;
                case "PONG": return IRCCommand.Pong;
                case "HOSTTARGET": return IRCCommand.HostTarget;
                case "CLEARCHAT": return IRCCommand.ClearChat;
                case "USERSTATE": return IRCCommand.UserState;
                case "GLOBALUSERSTATE": return IRCCommand.GlobalUserState;
                case "NICK": return IRCCommand.Nick;
                case "JOIN": return IRCCommand.Join;
                case "PART": return IRCCommand.Part;
                case "PASS": return IRCCommand.Pass;
                case "CAP": return IRCCommand.Cap;
                case "001": return IRCCommand._001;
                case "002": return IRCCommand._002;
                case "003": return IRCCommand._003;
                case "004": return IRCCommand._004;
                case "353": return IRCCommand._353;
                case "366": return IRCCommand._366;
                case "372": return IRCCommand._372;
                case "375": return IRCCommand._375;
                case "376": return IRCCommand._376;
                case "WHISPER": return IRCCommand.Whisper;
                case "SERVERCHANGE": return IRCCommand.ServerChange;
                case "RECONNECT": return IRCCommand.Reconnect;
                case "ROOMSTATE": return IRCCommand.RoomState;
                case "USERNOTICE": return IRCCommand.UserNotice;
                case "MODE": return IRCCommand.Mode;

                default:
                    return IRCCommand.Unknown;
            }
        }

        private enum ParserState
        {
            None,
            V3,
            Prefix,
            Command,
            Param,
            Trailing
        }

        public override string ToString()
        {
            StringBuilder raw = new StringBuilder(32);
            if (tags != null)
            {
                string[] temp_tags = new string[tags.Count];
                var i = 0;

                foreach (KeyValuePair<string, string> tag in tags)
                {
                    temp_tags[i] = tag.Key + "=" + tag.Value;
                    ++i;
                }

                if (temp_tags.Length > 0)
                {
                    raw.Append('@').Append(string.Join(';', temp_tags)).Append(' ');
                }
            }

            if (!string.IsNullOrEmpty(hostmask))
            {
                raw.Append(':').Append(hostmask).Append(' ');
            }

            raw.Append(ircCommand.ToString().ToUpper().Replace("_", ""));

            if (!string.IsNullOrEmpty(parameter))
            {
                raw.Append(' ').Append(parameter);
            }

            if (!string.IsNullOrEmpty(message))
            {
                raw.Append(" :").Append(message);
            }

            return raw.ToString();
        }
    }
}
