namespace BGC.Scripting;

public class Script
{
    public readonly string scriptText;
    private readonly List<ScriptDeclaration> scriptDeclarations = new List<ScriptDeclaration>();
    private readonly Dictionary<string, ScriptFunction> scriptFunctions = new Dictionary<string, ScriptFunction>();

    /// <summary>
    /// Parse Script
    /// </summary>
    /// <exception cref="ScriptParsingException"></exception>
    public Script(
        string scriptText,
        IEnumerator<Token> scriptTokens,
        params FunctionSignature[] expectedFunctions)
    {
        this.scriptText = scriptText;

        ScriptCompilationContext compilationContext = new ScriptCompilationContext();

        while (scriptTokens.Current is not EOFToken)
        {
            ParseNextGlobal(scriptTokens, compilationContext);
        }

        foreach (FunctionSignature data in expectedFunctions)
        {
            if (!scriptFunctions.ContainsKey(data.identifierToken.identifier))
            {
                throw new ScriptParsingException(
                    source: scriptTokens.Current ?? new EOFToken(0, 0),
                    message: $"Expected Function not found: {data}");
            }

            if (!data.Matches(scriptFunctions[data.identifierToken.identifier].functionSignature))
            {
                throw new ScriptParsingException(
                    source: scriptFunctions[data.identifierToken.identifier].functionSignature.identifierToken,
                    message: $"Expected Function: {data}  Found Function: {scriptFunctions[data.identifierToken.identifier].functionSignature}");
            }
        }

        foreach (ScriptFunction scriptFunction in scriptFunctions.Values)
        {
            scriptFunction.ParseFunctions(compilationContext);
        }
    }

    public bool HasFunction(string identifier) => scriptFunctions.ContainsKey(identifier);
    public FunctionSignature GetFunctionSignature(string identifier) => scriptFunctions[identifier].functionSignature;

    public bool HasFunction(FunctionSignature functionSignature)
    {
        string identifier = functionSignature.identifierToken.identifier;
        if (!scriptFunctions.ContainsKey(identifier))
        {
            return false;
        }

        return functionSignature.Matches(scriptFunctions[identifier].functionSignature);
    }

    public void ParseNextGlobal(
        IEnumerator<Token> scriptTokens,
        ScriptCompilationContext context)
    {
        switch (scriptTokens.Current)
        {
            case KeywordToken kwToken:
                //Valid operations:
                //  Global declaration
                //  Extern declaration
                //  Member declaration
                //  Class declaration
                switch (kwToken.keyword)
                {
                    case Keyword.Global:
                    case Keyword.Extern:
                        //Parse Global Declaration
                        {
                            scriptTokens.CautiousAdvance();

                            Type valueType = scriptTokens.ReadTypeAndAdvance();

                            IdentifierToken identToken = scriptTokens.GetTokenAndAdvance<IdentifierToken>();
                            IValueGetter? initializerExpression = null;

                            if (scriptTokens.TestAndConditionallySkip(Operator.Assignment))
                            {
                                initializerExpression = Expression.ParseNextGetterExpression(scriptTokens, context);
                            }

                            scriptTokens.AssertAndSkip(Separator.Semicolon, false);

                            scriptDeclarations.Add(
                                new GlobalDeclaration(
                                    identifierToken: identToken,
                                    valueType: valueType,
                                    isExtern: kwToken.keyword == Keyword.Extern,
                                    initializer: initializerExpression,
                                    context: context));
                        }
                        return;

                    case Keyword.Const:
                        {
                            scriptTokens.CautiousAdvance();

                            Type valueType = scriptTokens.ReadTypeAndAdvance();

                            IdentifierToken identToken = scriptTokens.GetTokenAndAdvance<IdentifierToken>();
                            scriptTokens.AssertAndSkip(Operator.Assignment);
                            IValueGetter initializerExpression = Expression.ParseNextGetterExpression(scriptTokens, context);

                            scriptTokens.AssertAndSkip(Separator.Semicolon, false);

                            if (initializerExpression is not LiteralToken litToken)
                            {
                                throw new ScriptParsingException(
                                    source: kwToken,
                                    message: $"The value of Const declarations must be constant");
                            }

                            object value = litToken.GetAs<object>();

                            if (!valueType.IsAssignableFrom(litToken.GetValueType()))
                            {
                                value = Convert.ChangeType(value, valueType);
                            }

                            context.DeclareConstant(
                                identifierToken: identToken,
                                type: valueType,
                                value: value);
                        }
                        return;

                    default:
                        throw new ScriptParsingException(
                            source: kwToken,
                            message: $"Token not valid for global context: {kwToken}.");
                }

            case TypeToken typeToken:
                //Parse Function or Member Declaration
                {
                    Type valueType = scriptTokens.ReadTypeAndAdvance();
                    IdentifierToken identToken = scriptTokens.GetTokenAndAdvance<IdentifierToken>();

                    if (scriptTokens.TestWithoutSkipping(Separator.OpenParen))
                    {
                        VariableData[] arguments = ParseArgumentsDeclaration(scriptTokens);

                        if (scriptFunctions.ContainsKey(identToken.identifier))
                        {
                            throw new ScriptParsingException(
                                source: identToken,
                                message: $"Two declarations of function {identToken.identifier} found.");
                        }

                        scriptFunctions.Add(identToken.identifier,
                            new ScriptFunction(
                                functionTokens: scriptTokens,
                                functionSignature: new FunctionSignature(
                                    identifierToken: identToken,
                                    returnType: valueType,
                                    arguments: arguments),
                                context: context));
                    }
                    else
                    {
                        //Member declaration
                        if (valueType == typeof(void))
                        {
                            throw new ScriptParsingException(
                                source: typeToken,
                                message: $"Cannot declare a member of type Void");
                        }

                        IValueGetter? initializerExpression = null;
                        if (scriptTokens.TestAndConditionallySkip(Operator.Assignment))
                        {
                            initializerExpression = Expression.ParseNextGetterExpression(scriptTokens, context);
                        }

                        scriptTokens.AssertAndSkip(Separator.Semicolon, false);

                        scriptDeclarations.Add(
                            new MemberDeclaration(
                                identifierToken: identToken,
                                valueType: valueType,
                                initializer: initializerExpression,
                                context: context));
                    }
                }

                break;

            default:
                throw new ScriptParsingException(
                    source: scriptTokens.Current,
                    message: $"Token not valid for global context: {scriptTokens.Current}.");

        }
    }

    public IEnumerable<KeyInfo> GetDeclarations()
    {
        foreach (ScriptDeclaration decl in scriptDeclarations)
        {
            if (decl is GlobalDeclaration globalDecl && !globalDecl.IsExtern)
            {
                yield return globalDecl.KeyInfo;
            }
        }
    }

    public IEnumerable<KeyInfo> GetDependencies()
    {
        foreach (ScriptDeclaration decl in scriptDeclarations)
        {
            if (decl is GlobalDeclaration globalDecl && globalDecl.IsExtern)
            {
                yield return globalDecl.KeyInfo;
            }
        }
    }

    /// <summary>
    /// Creates declared variables in the script context
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="ScriptRuntimeException"></exception>
    public ScriptRuntimeContext PrepareScript(GlobalRuntimeContext context)
    {
        ScriptRuntimeContext scriptContext = new ScriptRuntimeContext(context, this);

        foreach (ScriptDeclaration declaration in scriptDeclarations)
        {
            declaration.Execute(scriptContext);
        }

        return scriptContext;
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    public void ExecuteFunction(
        string functionName,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }

        scriptFunctions[functionName].Execute(context, CancellationToken.None, arguments);
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    public T ExecuteFunction<T>(
        string functionName,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }

        scriptFunctions[functionName].Execute(context, CancellationToken.None, arguments);

        return context.PopReturnValue<T>();
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public T ExecuteFunction<T>(
        string functionName,
        int timeoutMS,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }

        using CancellationTokenSource tokenSource = new CancellationTokenSource(timeoutMS);

        scriptFunctions[functionName].Execute(context, tokenSource.Token, arguments);
        return context.PopReturnValue<T>();
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public Task ExecuteFunctionAsync(
        string functionName,
        int timeoutMS,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }

        return Task.Run(() =>
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource(timeoutMS);
            scriptFunctions[functionName].Execute(context, tokenSource.Token, arguments);
        });
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public Task<T> ExecuteFunctionAsync<T>(
        string functionName,
        int timeoutMS,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }

        return Task.Run(() =>
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource(timeoutMS);
            scriptFunctions[functionName].Execute(context, tokenSource.Token, arguments);
            return context.PopReturnValue<T>();
        });
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
#pragma warning disable CA1068 // CancellationToken parameters must come last
    public Task<T> ExecuteFunctionAsync<T>(
#pragma warning restore CA1068 // CancellationToken parameters must come last
        string functionName,
        int timeoutMS,
        CancellationToken ct,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }
        
        using CancellationTokenSource tokenSource = new CancellationTokenSource(timeoutMS);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, ct);
        
        return Task.Run(() =>
        {
            scriptFunctions[functionName].Execute(context, linkedSource.Token, arguments);
            return context.PopReturnValue<T>();
        },
        linkedSource.Token);
    }

    /// <summary>
    /// Executes the named function
    /// </summary>
    /// <exception cref="ScriptRuntimeException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
#pragma warning disable CA1068 // CancellationToken parameters must come last
    public Task ExecuteFunctionAsync(
#pragma warning restore CA1068 // CancellationToken parameters must come last
        string functionName,
        int timeoutMS,
        CancellationToken ct,
        ScriptRuntimeContext context,
        params object[] arguments)
    {
        if (!scriptFunctions.ContainsKey(functionName))
        {
            throw new ScriptRuntimeException($"Unable to find function {functionName} for external invocation.");
        }

        using CancellationTokenSource tokenSource = new CancellationTokenSource(timeoutMS);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, ct);

        return Task.Run(() =>
        {
            scriptFunctions[functionName].Execute(context, linkedSource.Token, arguments);
        },
        linkedSource.Token);
    }

    private static VariableData[] ParseArgumentsDeclaration(IEnumerator<Token> tokens)
    {
        List<VariableData> arguments = new List<VariableData>();
        tokens.AssertAndSkip(Separator.OpenParen);

        if (!tokens.TestWithoutSkipping(Separator.CloseParen))
        {
            do
            {
                Type argumentType = tokens.ReadTypeAndAdvance();
                IdentifierToken identToken = tokens.GetTokenAndAdvance<IdentifierToken>();

                arguments.Add(new VariableData(identToken, argumentType));
            }
            while (tokens.TestAndConditionallySkip(Separator.Comma));
        }

        tokens.AssertAndSkip(Separator.CloseParen);

        return arguments.ToArray();
    }
}
