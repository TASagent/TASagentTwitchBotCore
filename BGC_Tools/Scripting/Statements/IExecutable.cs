namespace BGC.Scripting;

public interface IExecutable : IExpression
{
    FlowState Execute(ScopeRuntimeContext context, CancellationToken ct);
}
