using System.Reflection;

namespace BGC.Scripting.Parsing;

public abstract class RegisteredMethodOperation : IValueGetter, IExecutable
{
    private readonly InvocationArgument[] args;
    private readonly MethodInfo methodInfo;
    private readonly Type returnType;
    private readonly Token source;

    protected abstract object? GetInstanceValue(RuntimeContext context);

    public RegisteredMethodOperation(
        InvocationArgument[] args,
        MethodInfo methodInfo,
        Token source)
    {
        this.args = args;
        this.source = source;
        this.methodInfo = methodInfo;

        returnType = methodInfo.ReturnType;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableOrConvertableFromType(this.returnType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve result of Method Invocation with type {this.returnType.Name} as type {returnType.Name}");
        }

        object?[] argumentValues = args.GetArgs(methodInfo, context);

        object? result = methodInfo.Invoke(
            obj: GetInstanceValue(context),
            parameters: argumentValues);

        //Handles By-Ref arguments
        args.HandlePostInvocation(argumentValues, context);

        if (!returnType.IsAssignableFrom(this.returnType))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)result;
    }

    public FlowState Execute(
        ScopeRuntimeContext context,
        CancellationToken ct)
    {
        object?[] argumentValues = args.GetArgs(methodInfo, context);

        methodInfo.Invoke(
            obj: GetInstanceValue(context),
            parameters: argumentValues);

        //Handles By-Ref arguments
        args.HandlePostInvocation(argumentValues, context);

        return FlowState.Nominal;
    }

    public Type GetValueType() => returnType;
    public override string ToString() => $"{GetType()}: From {source}.";

    private static MethodInfo? FindMatchingMethod(
        MethodInfo[] methodInfos,
        Type[] parameterTypes)
    {
        if (Type.DefaultBinder.SelectMethod(
            bindingAttr: BindingFlags.Public | BindingFlags.Instance,
            match: methodInfos,
            types: parameterTypes,
            modifiers: null) is MethodInfo methodInfo)
        {
            return methodInfo;
        }

        foreach (MethodInfo genericMethodInfo in methodInfos.Where(x => x.IsGenericMethod))
        {

        }

        return null;
    }

    private static bool CompareGenericMethod(
        MethodInfo methodInfo,
        Type[] parameterTypes)
    {
        if (!methodInfo.IsGenericMethod)
        {
            return false;
        }

        ParameterInfo[] parameters = methodInfo.GetParameters();

        if (parameterTypes.Length > parameters.Length)
        {
            return false;
        }

        if (methodInfo.ContainsGenericParameters)
        {
            foreach (Type type in methodInfo.GetGenericArguments())
            {

            }
        }

        for (int i = 0; i < parameterTypes.Length; i++)
        {
            if (!parameters[i].ParameterType.AssignableOrConvertableFromType(parameterTypes[i]))
            {
                return false;
            }
        }

        return true;
    }
}

public class RegisteredInstanceMethodOperation : RegisteredMethodOperation
{
    private readonly IValueGetter value;

    protected override object? GetInstanceValue(RuntimeContext context) => value.GetAs<object>(context);

    public RegisteredInstanceMethodOperation(
        IValueGetter value,
        InvocationArgument[] args,
        MethodInfo methodInfo,
        Token source)
        : base(args, methodInfo, source)
    {
        this.value = value;
    }
}


public class RegisteredStaticMethodOperation : RegisteredMethodOperation
{
    protected override object? GetInstanceValue(RuntimeContext context) => null;

    public RegisteredStaticMethodOperation(
        InvocationArgument[] args,
        MethodInfo methodInfo,
        Token source)
        : base(args, methodInfo, source)
    {
    }
}
