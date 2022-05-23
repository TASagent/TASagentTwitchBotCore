namespace BGC.Scripting;

public enum Keyword
{
    //Conditionals
    If = 0,
    ElseIf,
    Else,
    Switch,

    //Loops
    While,
    For,
    ForEach,
    In,

    //Flow Control
    Continue,
    Break,
    Return,
    Case,
    Default,

    //Declaration Modifiers
    Global,
    Extern,
    Const,

    //Static Types
    System,
    Debug,
    Math,

    //Construction keyword
    New,

    MAX
}
