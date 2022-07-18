﻿namespace BGC.Scripting;

/// <summary>
/// This class is used to track variable values during runtime
/// </summary>
public abstract class RuntimeContext
{
    private readonly RuntimeContext? parent;
    protected readonly Dictionary<string, ScriptValue> valueDictionary = new Dictionary<string, ScriptValue>();


    private GlobalRuntimeContext? globalContext = null;
    protected virtual GlobalRuntimeContext GlobalContext => globalContext ??= parent!.GlobalContext;

    private ScriptRuntimeContext? scriptContext = null;
    protected virtual ScriptRuntimeContext ScriptContext => scriptContext ??= parent!.ScriptContext;

    private FunctionRuntimeContext? functionContext = null;
    protected virtual FunctionRuntimeContext FunctionContext => functionContext ??= parent!.FunctionContext;

    protected virtual RuntimeContext? ScopeParent => parent;

    public RuntimeContext(RuntimeContext? parent)
    {
        this.parent = parent;
    }

    public virtual void PushReturnValue(object? value) => GlobalContext.PushReturnValue(value);
    public virtual T PopReturnValue<T>() => GlobalContext.PopReturnValue<T>();
    public virtual bool SearchParent(string key) => true;

    public void DeclareVariable(string key, Type type, object? value)
    {
        if (valueDictionary.ContainsKey(key))
        {
            throw new ScriptRuntimeException($"Variable {key} already defined in context.");
        }

        valueDictionary.Add(
            key: key,
            value: new ScriptValue(
                valueType: type,
                key: key,
                value: value));
    }

    public bool VariableExists(string key) =>
        valueDictionary.ContainsKey(key) ||
        (SearchParent(key) && ScopeParent!.VariableExists(key));

    public T? GetExistingValue<T>(string key)
    {
        if (valueDictionary.ContainsKey(key))
        {
            if (!typeof(T).AssignableOrConvertableFromType(valueDictionary[key].valueType))
            {
                throw new ScriptRuntimeException($"Value {key} retrieved as {typeof(T).Name} when it's {valueDictionary[key].valueType.Name}");
            }

            if (!typeof(T).IsAssignableFrom(valueDictionary[key].valueType))
            {
                return (T?)Convert.ChangeType(valueDictionary[key].value, typeof(T));
            }

            return (T?)valueDictionary[key].value;
        }

        if (SearchParent(key))
        {
            return ScopeParent!.GetExistingValue<T>(key);
        }

        throw new ScriptRuntimeException($"Variable {key} was requested but never declared.");
    }

    public void SetExistingValue(string key, object? value)
    {
        if (valueDictionary.ContainsKey(key))
        {
            Type otherType = value is null ? typeof(NullLiteralToken) : value.GetType();

            if (!valueDictionary[key].valueType.AssignableOrConvertableFromType(otherType))
            {
                throw new ScriptRuntimeException($"Value {key} set as {otherType.Name} when it's {valueDictionary[key].valueType.Name}");
            }

            if (valueDictionary[key].valueType.IsAssignableFrom(otherType))
            {
                valueDictionary[key] = valueDictionary[key].UpdateValue(value);
            }
            else
            {
                valueDictionary[key] = valueDictionary[key].UpdateValue(
                    Convert.ChangeType(value, valueDictionary[key].valueType));
            }
            return;
        }

        if (SearchParent(key))
        {
            ScopeParent!.SetExistingValue(key, value);
            return;
        }

        throw new ScriptRuntimeException($"Variable {key} was set but never declared.");
    }

    public virtual void RunVoidFunction(string functionName, object?[] arguments) =>
        ScriptContext.RunVoidFunction(functionName, arguments);

    public virtual object RunFunction(string functionName, object?[] arguments) =>
        ScriptContext.RunFunction(functionName, arguments);


    public virtual void RunVoidFunction(Guid functionId, object?[] arguments) =>
        ScriptContext.RunVoidFunction(functionId, arguments);

    public virtual object RunFunction(Guid functionId, object?[] arguments) =>
        ScriptContext.RunFunction(functionId, arguments);

    public virtual void Clear() => valueDictionary.Clear();
}

public class GlobalRuntimeContext : RuntimeContext
{
    private object? stashedReturnValue = null;

    public GlobalRuntimeContext()
        : base(null)
    {
    }

    protected override GlobalRuntimeContext GlobalContext => this;
    protected override ScriptRuntimeContext ScriptContext =>
        throw new NotSupportedException("Cannot retrieve ScriptContext from GlobalContext");
    protected override FunctionRuntimeContext FunctionContext =>
        throw new NotSupportedException("Cannot retrieve FunctionContext from GlobalContext");

    public override void RunVoidFunction(string functionName, object?[] arguments) =>
        throw new NotSupportedException("Cannot Run Functions from GlobalContext");
    public override object RunFunction(string functionName, object?[] arguments) =>
        throw new NotSupportedException("Cannot Run Functions from GlobalContext");

    public override void PushReturnValue(object? value) => stashedReturnValue = value;
    public override T PopReturnValue<T>()
    {
        Type returnValueType = stashedReturnValue is null ? typeof(NullLiteralToken) : stashedReturnValue.GetType();

        if (!typeof(T).AssignableOrConvertableFromType(returnValueType))
        {
            throw new ScriptRuntimeException($"Unable to return value of type {returnValueType.Name} as a {typeof(T).Name}");
        }

        if (typeof(T).IsAssignableFrom(returnValueType))
        {
            T temp = (T)stashedReturnValue!;
            stashedReturnValue = null;
            return temp!;
        }
        else
        {
            T temp = (T)Convert.ChangeType(stashedReturnValue, typeof(T))!;
            stashedReturnValue = null;
            return temp!;
        }
    }

    public override bool SearchParent(string key) => false;

    public void AddOrSetValue(string key, Type type, object value)
    {
        if (valueDictionary.ContainsKey(key))
        {
            if (!valueDictionary[key].valueType.AssignableOrConvertableFromType(type))
            {
                throw new ScriptRuntimeException($"Value {key} set as {type.Name} when it was {valueDictionary[key].valueType.Name}");
            }

            if (valueDictionary[key].valueType.IsAssignableFrom(type))
            {
                valueDictionary[key] = valueDictionary[key].UpdateValue(value);
            }
            else
            {
                valueDictionary[key] = valueDictionary[key].UpdateValue(
                    Convert.ChangeType(value, valueDictionary[key].valueType));
            }
        }
        else
        {
            valueDictionary.Add(key, new ScriptValue(type, key, value));
        }
    }

    public object? GetRawValue(string key)
    {
        if (valueDictionary.ContainsKey(key))
        {
            return valueDictionary[key].value;
        }

        throw new Exception($"ScriptContext dictionary did not contain key: {key}");
    }

    public Type GetValueType(string identifier)
    {
        if (!valueDictionary.ContainsKey(identifier))
        {
            throw new Exception($"Tried to get global Type of a non-existing global: {identifier}");
        }

        return valueDictionary[identifier].valueType;
    }

    public override void Clear()
    {
        base.Clear();
        stashedReturnValue = null;
    }
}

public class ScriptRuntimeContext : RuntimeContext
{
    readonly HashSet<string> globalDeclarations = new HashSet<string>();

    private readonly Script script;

    protected override ScriptRuntimeContext ScriptContext => this;
    protected override FunctionRuntimeContext FunctionContext =>
        throw new NotSupportedException("Cannot retrieve FunctionContext from ScriptContext");

    public ScriptRuntimeContext(GlobalRuntimeContext globalContext, Script script)
        : base(globalContext)
    {
        this.script = script;
    }

    public bool GlobalVariableExists(string identifier) => GlobalContext.VariableExists(identifier);

    //Only search global context for declared globals
    public override bool SearchParent(string key) => globalDeclarations.Contains(key);

    public void DeclareExistingGlobal(string key, Type type)
    {
        if (!GlobalVariableExists(key))
        {
            throw new Exception($"Tried to DeclareExistingGlobal on a non-existing global");
        }

        if (type != GlobalContext.GetValueType(key))
        {
            throw new ScriptRuntimeException($"Tried to access an existing global ({key}) with the wrong type.  DeclaredType: {type.Name}  ActualType: {GlobalContext.GetValueType(key).Name}.");
        }

        globalDeclarations.Add(key);
    }

    public void DeclareNewGlobal(string key, Type type, object? value)
    {
        globalDeclarations.Add(key);
        GlobalContext.DeclareVariable(key, type, value);
    }

    public override void RunVoidFunction(string functionName, object?[] arguments) =>
        script.ExecuteFunction(functionName, this, arguments);

    public override object RunFunction(string functionName, object?[] arguments) =>
        script.ExecuteFunction<object>(functionName, this, arguments);


    public override void RunVoidFunction(Guid functionId, object?[] arguments) =>
        script.ExecuteFunction(functionId, this, arguments);

    public override object RunFunction(Guid functionId, object?[] arguments) =>
        script.ExecuteFunction<object>(functionId, this, arguments);
}

public class FunctionRuntimeContext : RuntimeContext
{
    protected override FunctionRuntimeContext FunctionContext => this;
    protected override RuntimeContext ScopeParent => ScriptContext;

    public FunctionRuntimeContext(
        ScriptRuntimeContext scriptContext,
        in FunctionSignature functionSignature,
        object?[] arguments)
        : base(scriptContext)
    {

        if (arguments.Length != functionSignature.arguments.Length)
        {
            throw new ScriptRuntimeException(
                $"Tried to call function {functionSignature} with argument list: {string.Join(", ", arguments.Select(x => x?.ToString() ?? "null"))}");
        }

        for (int i = 0; i < functionSignature.arguments.Length; i++)
        {
            if (arguments[i] is not null &&
                !functionSignature.arguments[i].valueType.AssignableOrConvertableFromType(arguments[i]!.GetType()))
            {
                throw new ScriptRuntimeException(
                    $"Incompatible argument type for argument {functionSignature.arguments[i].identifierToken.identifier}.  " +
                    $"Expected: {functionSignature.arguments[i].valueType.Name},  Received: {arguments[i]!.GetType().Name}");
            }

            DeclareVariable(
                key: functionSignature.arguments[i].identifierToken.identifier,
                type: functionSignature.arguments[i].valueType,
                value: arguments[i]);
        }
    }
}

public class ScopeRuntimeContext : RuntimeContext
{
    public ScopeRuntimeContext(RuntimeContext context)
        : base(context)
    {
        if (!(context is FunctionRuntimeContext || context is ScopeRuntimeContext))
        {
            throw new ArgumentException($"ScopeRuntimeContext expects either a Function- or " +
                $"Scope-Context parent.  Received: {context}");
        }
    }
}

public readonly struct ScriptValue
{
    public readonly Type valueType;
    public readonly string key;
    public readonly object? value;

    public ScriptValue(
        Type valueType,
        string key,
        object? value)
    {
        this.valueType = valueType;
        this.key = key;
        this.value = value;
    }

    public ScriptValue UpdateValue(object? newValue) => new ScriptValue(
        valueType: valueType,
        key: key,
        value: newValue);
}
