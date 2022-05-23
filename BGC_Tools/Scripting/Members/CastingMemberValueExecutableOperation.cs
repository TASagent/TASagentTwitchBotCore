﻿namespace BGC.Scripting;

public class CastingMemberValueExecutableOperation : IValueGetter, IExecutable
{
    private readonly IValueGetter value;
    private readonly Type outputType;
    private readonly Func<object, object?> operation;

    public CastingMemberValueExecutableOperation(
        IValueGetter value,
        Type outputType,
        Func<object, object?> operation)
    {
        this.value = value;
        this.outputType = outputType;
        this.operation = operation;
    }

    public FlowState Execute(ScopeRuntimeContext context, CancellationToken ct)
    {
        operation(value.GetAs<object>(context)!);

        return FlowState.Nominal;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableFromType(outputType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve result of Indexing with type {outputType.Name} as type {returnType.Name}");
        }

        object? result = operation(value.GetAs<object>(context)!);

        if (!returnType.IsAssignableFrom(outputType))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)result;
    }

    public Type GetValueType() => outputType;
}

