﻿namespace BGC.Scripting;

public class IdentifierExpression : IValue
{
    public readonly string identifier;
    public readonly Type type;

    public IdentifierExpression(
        IdentifierToken identifierToken,
        CompilationContext context)
    {
        identifier = identifierToken.identifier;
        type = context.GetValueType(identifier)!;

        if (type == null)
        {
            throw new ScriptParsingException(
                source: identifierToken,
                message: $"Identifier {identifier} used without first declaring it.");
        }
    }

    public T? GetAs<T>(RuntimeContext context)
    {
        if (typeof(T).AssignableOrConvertableFromType(type))
        {
            return context.GetExistingValue<T>(identifier);
        }

        throw new ScriptRuntimeException(
            $"Unable to implicitly cast identifier {identifier} of type {type.Name} to type {typeof(T).Name}");
    }

    public void Set(RuntimeContext context, object? value)
    {
        Type otherType = (value is null) ? typeof(NullLiteralToken) : value.GetType();

        if (type.AssignableOrConvertableFromType(otherType))
        {
            context.SetExistingValue(identifier, value);
        }
        else
        {
            throw new ScriptRuntimeException(
                $"Unable to set identifier {identifier} of type {type.Name} to value {value} of type {otherType.Name}");
        }
    }

    public void SetAs<T>(RuntimeContext context, T? value)
    {
        if (type.AssignableOrConvertableFromType(typeof(T)))
        {
            context.SetExistingValue(identifier, value);
        }
        else
        {
            throw new ScriptRuntimeException(
                $"Unable to set identifier {identifier} of type {type.Name} to value {value} of type {typeof(T).Name}");
        }
    }

    public Type GetValueType() => type;

    public override string ToString() => identifier;
}
