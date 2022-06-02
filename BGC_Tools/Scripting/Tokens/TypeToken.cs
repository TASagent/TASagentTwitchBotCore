namespace BGC.Scripting;

public class TypeToken : Token
{
    public readonly Type type;
    public readonly string alias;
    public bool IsGenericType => type.ContainsGenericParameters;

    public TypeToken(int line, int column, string alias, Type type)
        : base(line, column)
    {
        this.type = type;
        this.alias = alias;
    }

    public TypeToken(Token source, string alias, Type type)
        : base(source)
    {
        this.type = type;
        this.alias = alias;
    }

    public override string ToString() => type.ToString();
}
