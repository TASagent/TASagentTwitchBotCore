using System.Reflection;

namespace BGC.Scripting.Parsing;

public abstract class RegisteredMethodOperation : IValueGetter, IExecutable
{
    private readonly IValueGetter[] args;
    private readonly MethodInfo methodInfo;
    private readonly Type returnType;
    private readonly Token source;

    protected abstract object? GetInstanceValue(RuntimeContext context);

    public RegisteredMethodOperation(
        IValueGetter[] args,
        MethodInfo methodInfo,
        Token source)
    {
        this.args = args;
        this.source = source;
        this.methodInfo = methodInfo;

        //Type[] argumentTypes = args.Select(x => x.GetValueType()).ToArray();

        //MethodInfo? matchingMethod = Type.DefaultBinder.SelectMethod(
        //    BindingFlags.Public | BindingFlags.Instance,
        //    methodInfos,
        //    args.Select(x => x.GetValueType()).ToArray(),
        //    null) as MethodInfo;

        //if (matchingMethod is null)
        //{
        //    matchingMethod = methodInfos
        //        .FirstOrDefault(x => CompareGenericMethod(x, argumentTypes));
        //}

        //if (matchingMethod is null)
        //{
        //    throw new ScriptParsingException(
        //        source: source,
        //        message: $"No matching method \"{methodInfos[0].Name}\" registered with arguments: {string.Join(", ", argumentTypes.Select(x => x.Name))}.");
        //}

        returnType = methodInfo.ReturnType;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableOrConvertableFromType(this.returnType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve result of Method Invocation with type {this.returnType.Name} as type {returnType.Name}");
        }

        object? result = methodInfo.Invoke(
            obj: GetInstanceValue(context),
            parameters: HandleArgList(methodInfo, args, context));

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
        methodInfo.Invoke(
            obj: GetInstanceValue(context),
            parameters: HandleArgList(methodInfo, args, context));

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

    private static object?[] HandleArgList(
        MethodInfo methodInfo,
        IValueGetter[] args,
        RuntimeContext context)
    {
        ParameterInfo[] parameters = methodInfo.GetParameters();
        object?[] argList = new object?[parameters.Length];

        if (parameters.Length != args.Length)
        {
            throw new ScriptRuntimeException($"Mismatched parameters for method.");
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (parameters[i].ParameterType.IsAssignableFrom(args[i].GetValueType()))
            {
                argList[i] = args[i].GetAs<object>(context);
            }
            else
            {
                argList[i] = Convert.ChangeType(args[i].GetAs<object>(context), parameters[i].ParameterType);
            }
        }

        return argList;
    }
}

public class RegisteredInstanceMethodOperation : RegisteredMethodOperation
{
    private readonly IValueGetter value;

    protected override object? GetInstanceValue(RuntimeContext context) => value.GetAs<object>(context);

    public RegisteredInstanceMethodOperation(
        IValueGetter value,
        IValueGetter[] args,
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
        IValueGetter[] args,
        MethodInfo methodInfo,
        Token source)
        : base(args, methodInfo, source)
    {
    }
}
