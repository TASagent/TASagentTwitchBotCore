namespace BGC.Scripting;

public static class ArgumentExtensions
{
    public static object[] GetArgs(
        this IValueGetter[] args,
        FunctionSignature functionSignature,
        RuntimeContext context)
    {
        object[] values = new object[functionSignature.arguments.Length];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = args[i].GetAs<object>(context)!;
            if (!functionSignature.arguments[i].valueType.IsAssignableFrom(args[i].GetValueType()))
            {
                values[i] = Convert.ChangeType(values[i], functionSignature.arguments[i].valueType);
            }
        }

        return values;
    }
}
