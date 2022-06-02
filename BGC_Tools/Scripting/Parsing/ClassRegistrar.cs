using System.Collections;
using System.Reflection;
using BGC.Collections.Generic;

namespace BGC.Scripting.Parsing;

public static class ClassRegistrar
{
    public delegate IExpression MemberExpression(IValueGetter value, Token source);
    public delegate IExpression MethodExpression(IValueGetter value, IValueGetter[] args, Token source);
    public delegate IExpression StaticExpression(Token source);
    public delegate IExpression StaticMethodExpression(IValueGetter[] args, Token source);

    private static readonly Dictionary<Type, ClassRegistration> classLookup = new Dictionary<Type, ClassRegistration>();
    private static readonly Dictionary<string, Type> aliasLookup = new Dictionary<string, Type>();

    static ClassRegistrar()
    {
        TryRegisterClass(typeof(object), "object");
        TryRegisterClass(typeof(bool), "bool");
        TryRegisterClass(typeof(byte), "byte");
        TryRegisterClass(typeof(sbyte), "sbyte");
        TryRegisterClass(typeof(short), "short");
        TryRegisterClass(typeof(ushort), "ushort");
        TryRegisterClass(typeof(int), "int");
        TryRegisterClass(typeof(uint), "uint");
        TryRegisterClass(typeof(long), "long");
        TryRegisterClass(typeof(ulong), "ulong");
        TryRegisterClass(typeof(float), "float");
        TryRegisterClass(typeof(double), "double");
        TryRegisterClass(typeof(decimal), "decimal");
        TryRegisterClass(typeof(char), "char");

        TryRegisterClass(typeof(string), "string");

        TryRegisterClass(typeof(DateOnly));
        TryRegisterClass(typeof(DateTime));
        TryRegisterClass(typeof(DateTimeOffset));
        TryRegisterClass(typeof(TimeSpan));

        TryRegisterClass(typeof(Random));
        TryRegisterClass(typeof(Math));

        TryRegisterClass(typeof(IList));
        TryRegisterClass(typeof(IEnumerable));

        TryRegisterClass(typeof(IEnumerable<>));
        TryRegisterClass(typeof(IDepletable<>));
        TryRegisterClass(typeof(IList<>));

        TryRegisterClass(typeof(List<>));
        TryRegisterClass(typeof(Queue<>));
        TryRegisterClass(typeof(Stack<>));
        TryRegisterClass(typeof(HashSet<>));
        TryRegisterClass(typeof(Dictionary<,>));
        TryRegisterClass(typeof(DepletableList<>));
        TryRegisterClass(typeof(DepletableBag<>));
        TryRegisterClass(typeof(RingBuffer<>));
    }


    public static bool TryRegisterClass<T>(string registerAs = "", bool limited = false) =>
        TryRegisterClass(typeof(T), registerAs, limited);

    public static bool TryRegisterClass(
        Type type,
        string registerAs = "",
        bool limited = false)
    {
        if (classLookup.ContainsKey(type))
        {
            return false;
        }

        if (string.IsNullOrEmpty(registerAs))
        {
            registerAs = type.Name;

            if (registerAs.Contains('`'))
            {
                registerAs = registerAs[0..registerAs.IndexOf('`')];
            }
        }

        if (aliasLookup.ContainsKey(registerAs))
        {
            return false;
        }

        ClassRegistration newRegistration = new ClassRegistration(type, !limited);
        aliasLookup.Add(registerAs, type);
        classLookup.Add(type, newRegistration);

        return true;
    }

    public static Type? LookUpClass(string className) => aliasLookup.GetValueOrDefault(className);

    public static IEnumerable<(ClassRegistration registration, Type[]? genericArguments)> GetRegisteredClasses(this Type type)
    {
        foreach (Type baseClass in type.GetTypes())
        {
            if (classLookup.TryGetValue(baseClass, out ClassRegistration? registration))
            {
                yield return (registration, null);
            }

            if (baseClass.IsGenericType)
            {
                if (classLookup.TryGetValue(baseClass.GetGenericTypeDefinition(), out registration))
                {
                    yield return (registration, baseClass.GetGenericArguments());
                }
            }
        }
    }

    public static IExpression? GetMemberExpression(
        IValueGetter value,
        string memberName,
        Token source)
    {
        foreach ((ClassRegistration registration, Type[]? genericClassArguments) in value.GetValueType().GetRegisteredClasses())
        {
            if (registration.GetPropertyExpression(value, genericClassArguments, memberName, source) is IExpression propertyExpression)
            {
                return propertyExpression;
            }
        }

        return null;
    }

    public static IExpression? GetMethodExpression(
        IValueGetter value,
        Type[]? genericMethodArguments,
        IValueGetter[] args,
        string methodName,
        Token source)
    {
        foreach ((ClassRegistration registration, Type[]? genericClassArguments) in value.GetValueType().GetRegisteredClasses())
        {
            if (registration.GetMethodExpression(value, genericClassArguments, genericMethodArguments, args, methodName, source) is IExpression methodExpression)
            {
                return methodExpression;
            }
        }

        return null;
    }

    public static IExpression? GetStaticMethodExpression(
        Type type,
        Type[]? genericMethodArguments,
        IValueGetter[] args,
        string methodName,
        Token source)
    {
        foreach ((ClassRegistration registration, Type[]? genericClassArguments) in type.GetRegisteredClasses())
        {
            if (registration.GetStaticMethodExpression(genericClassArguments, genericMethodArguments, args, methodName, source) is IExpression methodExpression)
            {
                return methodExpression;
            }
        }

        return null;
    }

    public static IExpression? GetStaticExpression(
        Type type,
        string propertyName,
        Token source)
    {
        foreach ((ClassRegistration registration, Type[]? genericClassArguments) in type.GetRegisteredClasses())
        {
            if (registration.GetStaticPropertyExpression(genericClassArguments, propertyName, source) is IExpression propertyExpression)
            {
                return propertyExpression;
            }
        }

        return null;
    }

    private static IEnumerable<Type> GetTypes(this Type type) => type
        .GetBaseTypesAndInterfaces()
        .Prepend(type)
        .Distinct();

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
        private readonly bool fullRegistration;

        public ClassRegistration(
            Type type,
            bool fullRegistration)
        {
            this.type = type;
            this.fullRegistration = fullRegistration;
        }

        public IExpression? GetMethodExpression(
            IValueGetter value,
            Type[]? genericClassArguments,
            Type[]? genericMethodArguments,
            IValueGetter[] args,
            string methodName,
            Token source)
        {
            Type invocationType = type;

            if (type.ContainsGenericParameters)
            {
                if (genericClassArguments is null || genericClassArguments.Length == 0)
                {
                    //Can't construct concrete class
                    return null;
                }

                invocationType = type.MakeGenericType(genericClassArguments);
            }

            //Try to find it
            IEnumerable<MethodInfo> methodInfos = invocationType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.Name == methodName)
                .Where(x => fullRegistration || x.GetCustomAttribute<ScriptingAccessAttribute>() is not null);

            if (!methodInfos.Any())
            {
                return null;
            }

            MethodInfo? selectedMethodInfo = SelectMethod(
                methodInfos: methodInfos.ToArray(),
                argumentTypes: args.Select(x => x.GetValueType()).ToArray(),
                genericMethodArguments: genericMethodArguments);

            if (selectedMethodInfo is null)
            {
                return null;
            }

            return new RegisteredInstanceMethodOperation(
                value: value,
                args: args,
                methodInfo: selectedMethodInfo,
                source: source);
        }

        public IExpression? GetPropertyExpression(
            IValueGetter value,
            Type[]? genericClassArguments,
            string propertyName,
            Token source)
        {
            Type invocationType = type;

            if (type.ContainsGenericParameters)
            {
                if (genericClassArguments is null || genericClassArguments.Length == 0)
                {
                    //Can't construct concrete class
                    return null;
                }

                invocationType = type.MakeGenericType(genericClassArguments);
            }

            //Try to find it
            PropertyInfo? propertyInfo = invocationType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo is not null && (fullRegistration || propertyInfo.GetCustomAttribute<ScriptingAccessAttribute>() is not null))
            {
                if (propertyInfo.CanWrite)
                {
                    return new RegisteredSettableInstancePropertyOperation(
                        value: value,
                        propertyInfo: propertyInfo,
                        source: source);
                }
                else
                {
                    return new RegisteredGettableInstancePropertyOperation(
                        value: value,
                        propertyInfo: propertyInfo,
                        source: source);
                }
            }

            FieldInfo? fieldInfo = invocationType.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo is not null && (fullRegistration || fieldInfo.GetCustomAttribute<ScriptingAccessAttribute>() is not null))
            {
                return new RegisteredGettableInstanceFieldOperation(
                    value: value,
                    fieldInfo: fieldInfo,
                    source: source);
            }

            return null;
        }

        private static MethodInfo? SelectMethod(
            MethodInfo[] methodInfos,
            Type[] argumentTypes,
            Type[]? genericMethodArguments)
        {
            if (genericMethodArguments is null)
            {
                //Test Non-Generic
                if (Type.DefaultBinder.SelectMethod(
                    bindingAttr: BindingFlags.Public | BindingFlags.Instance,
                    match: methodInfos,
                    types: argumentTypes,
                    modifiers: null) is MethodInfo methodInfo)
                {
                    return methodInfo;
                }
            }
            else
            {
                //Find Generic
                if (Type.DefaultBinder.SelectMethod(
                    bindingAttr: BindingFlags.Public | BindingFlags.Instance,
                    match: methodInfos
                        .Where(x => x.ContainsGenericParameters)
                        .Select(x=>x.MakeGenericMethod(genericMethodArguments))
                        .ToArray(),
                    types: argumentTypes,
                    modifiers: null) is MethodInfo methodInfo)
                {
                    return methodInfo;
                }
            }

            //In principle, here is where we would do generic type inferencing

            return null;
        }

        public IExpression? GetStaticMethodExpression(
            Type[]? genericClassArguments,
            Type[]? genericMethodArguments,
            IValueGetter[] args,
            string methodName,
            Token source)
        {
            Type invocationType = type;

            if (type.ContainsGenericParameters)
            {
                if (genericClassArguments is null || genericClassArguments.Length == 0)
                {
                    //Can't construct concrete class
                    return null;
                }

                invocationType = type.MakeGenericType(genericClassArguments);
            }

            //Try to find it
            IEnumerable<MethodInfo> methodInfos = invocationType
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(x => x.Name == methodName)
                .Where(x => fullRegistration || x.GetCustomAttribute<ScriptingAccessAttribute>() is not null);

            if (!methodInfos.Any())
            {
                return null;
            }

            MethodInfo? selectedMethodInfo = SelectMethod(
                methodInfos: methodInfos.ToArray(),
                argumentTypes: args.Select(x => x.GetValueType()).ToArray(),
                genericMethodArguments: genericMethodArguments);

            if (selectedMethodInfo is null)
            {
                return null;
            }

            return new RegisteredStaticMethodOperation(
                args: args,
                methodInfo: selectedMethodInfo,
                source: source);
        }

        public IExpression? GetStaticPropertyExpression(
            Type[]? genericClassArguments,
            string propertyName,
            Token source)
        {
            Type invocationType = type;

            if (type.ContainsGenericParameters)
            {
                if (genericClassArguments is null || genericClassArguments.Length == 0)
                {
                    //Can't construct concrete class
                    return null;
                }

                invocationType = type.MakeGenericType(genericClassArguments);
            }

            //Try to find it
            PropertyInfo? propertyInfo = invocationType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (propertyInfo is not null && (fullRegistration || propertyInfo.GetCustomAttribute<ScriptingAccessAttribute>() is not null))
            {
                if (propertyInfo.CanWrite)
                {
                    return new RegisteredSettableStaticPropertyOperation(
                        propertyInfo: propertyInfo,
                        source: source);
                }
                else
                {
                    return new RegisteredGettableStaticPropertyOperation(
                        propertyInfo: propertyInfo,
                        source: source);
                }
            }

            FieldInfo? fieldInfo = invocationType.GetField(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (fieldInfo is not null && (fullRegistration || fieldInfo.GetCustomAttribute<ScriptingAccessAttribute>() is not null))
            {
                return new RegisteredGettableStaticFieldOperation(
                    fieldInfo: fieldInfo,
                    source: source);
            }

            return null;
        }
    }
}
