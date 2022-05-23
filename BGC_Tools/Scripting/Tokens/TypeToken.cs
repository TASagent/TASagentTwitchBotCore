namespace BGC.Scripting;

public class TypeToken : Token
{
    public readonly Type type;
    public readonly bool genericType;
    public readonly string alias;

    public TypeToken(int line, int column, string alias, Type type, bool genericType = false)
        : base(line, column)
    {
        this.type = type;
        this.alias = alias;
        this.genericType = genericType;
    }

    public TypeToken(Token source, string alias, Type type, bool genericType = false)
        : base(source)
    {
        this.type = type;
        this.alias = alias;
        this.genericType = genericType;
    }

    public override string ToString() => type.ToString();
}
