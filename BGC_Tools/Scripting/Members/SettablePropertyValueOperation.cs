namespace BGC.Scripting;

public class SettablePropertyValueOperation<TInput, TResult> : IValue
{
    private readonly IValueGetter value;
    private readonly Func<TInput, TResult?> getOperation;
    private readonly Action<TInput, TResult?> setOperation;

    public SettablePropertyValueOperation(
        IValueGetter value,
        Func<TInput, TResult?> getOperation,
        Action<TInput, TResult?> setOperation,
        Token source)
    {
        this.value = value;
        this.getOperation = getOperation;
        this.setOperation = setOperation;

        if (!typeof(TInput).AssignableFromType(value.GetValueType()))
        {
            throw new ScriptParsingException(
                source: source,
                message: $"Incorrect value type.  Expected: {typeof(TInput).Name}.  Received: {value.GetValueType().Name}. ");
        }
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableFromType(typeof(TResult)))
        {
            throw new ScriptRuntimeException($"Tried to retrieve result of Indexing with type {typeof(TResult).Name} as type {returnType.Name}");
        }

        TResult? result = getOperation(value.GetAs<TInput>(context)!);

        if (!returnType.IsAssignableFrom(typeof(TResult)))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)(object?)result;
    }

    public void Set(RuntimeContext context, object? newValue)
    {
        Type inputType = newValue?.GetType() ?? typeof(object);

        if (!typeof(TResult).AssignableFromType(inputType))
        {
            throw new ScriptRuntimeException($"Tried to set result of Indexing with type {typeof(TResult).Name} as type {inputType.Name}");
        }

        TResult? convertedValue;

        if (typeof(TResult).IsAssignableFrom(inputType))
        {
            convertedValue = (TResult?)newValue;
        }
        else
        {
            convertedValue = (TResult?)Convert.ChangeType(newValue, typeof(TResult));
        }

        setOperation(
            value.GetAs<TInput>(context)!,
            convertedValue);
    }

    public void SetAs<T>(RuntimeContext context, T? newValue)
    {
        Type inputType = typeof(T);

        if (!typeof(TResult).AssignableFromType(inputType))
        {
            throw new ScriptRuntimeException($"Tried to set result of Indexing with type {typeof(TResult).Name} as type {inputType.Name}");
        }

        TResult? convertedValue;

        if (typeof(TResult).IsAssignableFrom(inputType))
        {
            convertedValue = (TResult?)(object?)newValue;
        }
        else
        {
            convertedValue = (TResult?)Convert.ChangeType(newValue, typeof(TResult));
        }

        setOperation(
            value.GetAs<TInput>(context)!,
            convertedValue);
    }

    public Type GetValueType() => typeof(TResult);

}

