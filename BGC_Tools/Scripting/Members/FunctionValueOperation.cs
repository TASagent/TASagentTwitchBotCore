namespace BGC.Scripting;

public class FunctionValueOperation : IValueGetter, IExecutable
{
    private readonly Type outputType;
    private readonly Func<RuntimeContext, object?> operation;

    public FunctionValueOperation(
        Type outputType,
        Func<RuntimeContext, object?> operation)
    {
        this.outputType = outputType;
        this.operation = operation;
    }

    public FlowState Execute(ScopeRuntimeContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        operation(context);

        return FlowState.Nominal;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableFromType(outputType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve result of Indexing with type {outputType.Name} as type {returnType.Name}");
        }

        object? result = operation(context);

        if (!returnType.IsAssignableFrom(outputType))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)result;
    }

    public Type GetValueType() => outputType;
}

