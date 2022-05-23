namespace BGC.Scripting;

public class FunctionExecutableOperation : IExecutable
{
    private readonly Action<RuntimeContext> operation;

    public FunctionExecutableOperation(
        Action<RuntimeContext> operation)
    {
        this.operation = operation;
    }

    public FlowState Execute(ScopeRuntimeContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        operation(context);

        return FlowState.Nominal;
    }
}

