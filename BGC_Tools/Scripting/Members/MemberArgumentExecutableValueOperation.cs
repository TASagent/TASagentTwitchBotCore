namespace BGC.Scripting;

public class MemberArgumentValueExecutableOperation<TInput, TResult> : IValueGetter, IExecutable
{
    private readonly IValueGetter value;
    private readonly Func<TInput, RuntimeContext, TResult?> operation;

    public MemberArgumentValueExecutableOperation(
        IValueGetter value,
        Func<TInput, RuntimeContext, TResult?> operation,
        Token source)
    {
        this.value = value;
        this.operation = operation;

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

        TResult? result = operation(value.GetAs<TInput>(context)!, context);

        if (!returnType.IsAssignableFrom(typeof(TResult)))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)(object?)result;
    }

    public FlowState Execute(ScopeRuntimeContext context, CancellationToken ct)
    {
        operation(value.GetAs<TInput>(context)!, context);
        return FlowState.Nominal;
    }

    public Type GetValueType() => typeof(TResult);
}

