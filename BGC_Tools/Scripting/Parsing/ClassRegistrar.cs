using System.Collections;
using BGC.Collections.Generic;

namespace BGC.Scripting.Parsing;

public static partial class ClassRegistrar
{
    public delegate IExpression MemberExpression(IValueGetter value, Token source);
    public delegate IExpression MethodExpression(IValueGetter value, IValueGetter[] args, Token source);
    public delegate IExpression StaticExpression(Token source);
    public delegate IExpression StaticMethodExpression(IValueGetter[] args, Token source);

    private static readonly Dictionary<string, Type> aliasLookup = new Dictionary<string, Type>();

    private static readonly Dictionary<Type, IRegistration> classLookup = new Dictionary<Type, IRegistration>();

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

        aliasLookup.Add(registerAs, type);

        if (type.IsEnum)
        {
            classLookup.Add(type, new EnumRegistration(type));
        }
        else
        {
            classLookup.Add(type, new ClassRegistration(type, !limited));
        }

        return true;
    }

    public static Type? LookUpClass(string className) => aliasLookup.GetValueOrDefault(className);

    //public static bool HasMember(this Type type, string memberName)
    //{
    //    foreach ((IRegistration registration, Type[]? _) in type.GetRegisteredClasses())
    //    {
    //        if (registration.HasMember(memberName))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    public static IEnumerable<(IRegistration registration, Type[]? genericArguments)> GetRegisteredClasses(this Type type)
    {
        foreach (Type baseClass in type.GetTypes())
        {
            if (classLookup.TryGetValue(baseClass, out IRegistration? registration))
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
        foreach ((IRegistration registration, Type[]? genericClassArguments) in value.GetValueType().GetRegisteredClasses())
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
        InvocationArgument[] args,
        string methodName,
        Token source)
    {
        foreach ((IRegistration registration, Type[]? genericClassArguments) in value.GetValueType().GetRegisteredClasses())
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
        InvocationArgument[] args,
        string methodName,
        Token source)
    {
        foreach ((IRegistration registration, Type[]? genericClassArguments) in type.GetRegisteredClasses())
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
        foreach ((IRegistration registration, Type[]? genericClassArguments) in type.GetRegisteredClasses())
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

    public interface IRegistration
    {
        Type ClassType { get; }

        //bool HasMember(string memberName);

        IExpression? GetMethodExpression(
            IValueGetter value,
            Type[]? genericClassArguments,
            Type[]? genericMethodArguments,
            InvocationArgument[] args,
            string methodName,
            Token source);

        IExpression? GetPropertyExpression(
            IValueGetter value,
            Type[]? genericClassArguments,
            string propertyName,
            Token source);

        IExpression? GetStaticMethodExpression(
            Type[]? genericClassArguments,
            Type[]? genericMethodArguments,
            InvocationArgument[] args,
            string methodName,
            Token source);

        IExpression? GetStaticPropertyExpression(
            Type[]? genericClassArguments,
            string propertyName,
            Token source);
    }
}
