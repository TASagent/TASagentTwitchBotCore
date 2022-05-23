using System.Reflection;

namespace BGC.Scripting.Parsing;

public static class ClassRegistrar
{
    public delegate IExpression MemberExpression(IValueGetter value, Token source);
    public delegate IExpression MethodExpression(IValueGetter value, IValueGetter[] args, Token source);

    private static readonly Dictionary<Type, ClassRegistration> classLookup = new Dictionary<Type, ClassRegistration>();
    private static readonly Dictionary<string, Type> aliasLookup = new Dictionary<string, Type>();

    public static void RegisterClass<T>(string registerAs = "")
    {
        Type type = typeof(T);

        if (string.IsNullOrEmpty(registerAs))
        {
            registerAs = type.Name;
        }

        aliasLookup.Add(registerAs, type);
        classLookup.Add(type, new ClassRegistration(type));
    }

    public static bool TryRegisterClass<T>(string registerAs = "")
    {
        Type type = typeof(T);

        if (classLookup.ContainsKey(type))
        {
            return false;
        }

        if (string.IsNullOrEmpty(registerAs))
        {
            registerAs = type.Name;
        }

        if (aliasLookup.ContainsKey(registerAs))
        {
            return false;
        }

        ClassRegistration newRegistration = new ClassRegistration(type);
        aliasLookup.Add(registerAs, type);
        classLookup.Add(type, newRegistration);

        return true;
    }

    public static Type? LookUpClass(string className) => aliasLookup.GetValueOrDefault(className);

    public static IExpression? GetMemberExpression(
        IValueGetter value,
        string memberName,
        Token source)
    {
        foreach (Type baseClass in value.GetValueType().GetTypes())
        {
            if (classLookup.TryGetValue(baseClass, out ClassRegistration? registration))
            {
                if (registration.memberLookup.TryGetValue(memberName, out MemberExpression? member))
                {
                    return member.Invoke(value, source);
                }
            }
        }

        return null;
    }

    public static IExpression? GetMethodExpression(
        IValueGetter value,
        IValueGetter[] args,
        string methodName,
        Token source)
    {
        foreach (Type baseClass in value.GetValueType().GetTypes())
        {
            if (classLookup.TryGetValue(baseClass, out ClassRegistration? registration))
            {
                if (registration.methodLookup.TryGetValue(methodName, out MethodExpression? member))
                {
                    return member.Invoke(value, args, source);
                }
            }
        }

        return null;
    }

    private static IEnumerable<Type> GetTypes(this Type type) =>
        type.GetBaseTypesAndInterfaces().Prepend(type).Distinct();

    private static IEnumerable<Type> GetBaseTypesAndInterfaces(this Type type)
    {
        if (type.BaseType is null || type.BaseType == typeof(object))
        {
            return type.GetInterfaces();
        }

        return type.BaseType.GetBaseTypesAndInterfaces()
            .Prepend(type.BaseType)
            .Concat(type.GetInterfaces())
            .Distinct();
    }

    public class ClassRegistration
    {
        private readonly Type type;

        public readonly Dictionary<string, MemberExpression> memberLookup = new Dictionary<string, MemberExpression>();
        public readonly Dictionary<string, MethodExpression> methodLookup = new Dictionary<string, MethodExpression>();

        public ClassRegistration(
            Type type)
        {
            this.type = type;

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetCustomAttribute<ScriptingAccessAttribute>() is not null))
            {
                string memberName = property.Name;

                ScriptingAccessAttribute accessAttribute = property.GetCustomAttribute<ScriptingAccessAttribute>()!;

                if (!string.IsNullOrEmpty(accessAttribute.alias))
                {
                    memberName = accessAttribute.alias;
                }

                if (property.CanWrite)
                {
                    memberLookup.Add(memberName, (IValueGetter value, Token source) =>
                        new RegisteredSettablePropertyValueOperation(
                            value: value,
                            propertyInfo: property,
                            source: source));
                }
                else
                {
                    memberLookup.Add(memberName, (IValueGetter value, Token source) =>
                        new RegisteredGettablePropertyValueOperation(
                            value: value,
                            propertyInfo: property,
                            source: source));
                }
            }

            foreach (IGrouping<string, MethodInfo> methodGroup in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetCustomAttribute<ScriptingAccessAttribute>() is not null)
                .GroupBy(GetMethodName))
            {
                methodLookup.Add(methodGroup.Key, (IValueGetter value, IValueGetter[] args, Token source) =>
                    new RegisteredMethodValueOperation(
                        value: value,
                        args: args,
                        methodInfos: methodGroup.ToArray(),
                        source: source));
            }
        }

        private static string GetMethodName(MethodInfo methodInfo)
        {
            ScriptingAccessAttribute accessAttribute = methodInfo.GetCustomAttribute<ScriptingAccessAttribute>()!;
            if (!string.IsNullOrEmpty(accessAttribute.alias))
            {
                return accessAttribute.alias;
            }

            return methodInfo.Name;
        }

        public override string ToString() => $"Class Registration for {type.Name}";
    }
}


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public class ScriptingAccessAttribute : Attribute
{
    public readonly string? alias;

    public ScriptingAccessAttribute(string? alias = "")
    {
        this.alias = alias;
    }
}

public class RegisteredGettablePropertyValueOperation : IValueGetter
{
    private readonly IValueGetter value;
    private readonly PropertyInfo propertyInfo;
    private readonly Type propertyType;
    private readonly Token source;

    public RegisteredGettablePropertyValueOperation(
        IValueGetter value,
        PropertyInfo propertyInfo,
        Token source)
    {
        this.value = value;
        this.propertyInfo = propertyInfo;
        this.source = source;
        propertyType = propertyInfo.PropertyType;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableFromType(propertyType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve Property with type {propertyType.Name} as type {returnType.Name}");
        }

        object? result = propertyInfo.GetValue(value.GetAs<object>(context));

        if (!returnType.IsAssignableFrom(propertyType))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)result;
    }

    public Type GetValueType() => propertyType;

    public override string ToString() => $"{GetType()}: From {source}.";
}

public class RegisteredSettablePropertyValueOperation : IValue
{
    private readonly IValueGetter value;
    private readonly PropertyInfo propertyInfo;
    private readonly Type propertyType;
    private readonly Token source;

    public RegisteredSettablePropertyValueOperation(
        IValueGetter value,
        PropertyInfo propertyInfo,
        Token source)
    {
        this.value = value;
        this.propertyInfo = propertyInfo;
        this.source = source;
        propertyType = propertyInfo.PropertyType;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableFromType(propertyType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve Property with type {propertyType.Name} as type {returnType.Name}");
        }

        object? result = propertyInfo.GetValue(value.GetAs<object>(context));

        if (!returnType.IsAssignableFrom(propertyType))
        {
            return (T?)Convert.ChangeType(result, returnType);
        }

        return (T?)result;
    }

    public void Set(RuntimeContext context, object? newValue)
    {
        Type inputType = newValue?.GetType() ?? typeof(object);

        if (!propertyType.AssignableFromType(inputType))
        {
            throw new ScriptRuntimeException($"Tried to set result of Indexing with type {propertyType.Name} as type {inputType.Name}");
        }

        object? convertedValue = newValue;

        if (!propertyType.IsAssignableFrom(inputType))
        {
            convertedValue = Convert.ChangeType(convertedValue, propertyType);
        }

        propertyInfo.SetValue(value.GetAs<object>(context), convertedValue);
    }

    public void SetAs<T>(RuntimeContext context, T? newValue)
    {
        Type inputType = typeof(T);

        if (!propertyType.AssignableFromType(inputType))
        {
            throw new ScriptRuntimeException($"Tried to set result of Indexing with type {propertyType.Name} as type {inputType.Name}");
        }

        object? convertedValue = newValue;

        if (!propertyType.IsAssignableFrom(inputType))
        {
            convertedValue = Convert.ChangeType(convertedValue, propertyType);
        }

        propertyInfo.SetValue(value.GetAs<object>(context), convertedValue);
    }

    public Type GetValueType() => propertyType;
    public override string ToString() => $"{GetType()}: From {source}.";
}

public class RegisteredMethodValueOperation : IValueGetter, IExecutable
{
    private readonly IValueGetter value;
    private readonly IValueGetter[] args;
    private readonly MethodInfo methodInfo;
    private readonly Type returnType;
    private readonly Token source;

    public RegisteredMethodValueOperation(
        IValueGetter value,
        IValueGetter[] args,
        MethodInfo[] methodInfos,
        Token source)
    {
        this.value = value;
        this.args = args;
        this.source = source;

        Type[] argumentTypes = args.Select(x => x.GetValueType()).ToArray();

        MethodInfo? matchingMethod = methodInfos.FirstOrDefault(x => CompareMethod(x, argumentTypes));

        if (matchingMethod is null)
        {
            throw new ScriptParsingException(
                source: source,
                message: $"No matching method \"{methodInfos[0].Name}\" registered with arguments: {string.Join(", ", argumentTypes.Select(x => x.Name))}.");
        }

        methodInfo = matchingMethod;
        returnType = methodInfo.ReturnType;
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        Type returnType = typeof(T);

        if (!returnType.AssignableFromType(this.returnType))
        {
            throw new ScriptRuntimeException($"Tried to retrieve result of Method Invocation with type {this.returnType.Name} as type {returnType.Name}");
        }

        object? result = methodInfo.Invoke(
            obj: value.GetAs<object>(context),
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
            obj: value.GetAs<object>(context),
            parameters: HandleArgList(methodInfo, args, context));

        return FlowState.Nominal;
    }

    public Type GetValueType() => returnType;
    public override string ToString() => $"{GetType()}: From {source}.";

    private static bool CompareMethod(
        MethodInfo methodInfo,
        Type[] parameterTypes)
    {
        ParameterInfo[] parameters = methodInfo.GetParameters();

        if (parameterTypes.Length != parameters.Length)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].ParameterType.AssignableFromType(parameterTypes[i]))
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
