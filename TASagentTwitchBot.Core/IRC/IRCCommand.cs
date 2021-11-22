namespace TASagentTwitchBot.Core.IRC;

public enum IRCCommand
{
    Unknown,
    PrivMsg,
    Notice,
    Ping,
    Pong,
    Join,
    Part,
    HostTarget,
    ClearChat,
    UserState,
    GlobalUserState,
    Nick,
    Pass,
    Cap,
    _001,
    _002,
    _003,
    _004,
    _353,
    _366,
    _372,
    _375,
    _376,
    Whisper,
    RoomState,
    Reconnect,
    ServerChange,
    UserNotice,
    Mode
}
