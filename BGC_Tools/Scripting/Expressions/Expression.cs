namespace BGC.Scripting;

public static class Expression
{
    public static IValueGetter ParseNextGetterExpression(
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        Token currentToken = tokens.Current;
        IExpression? nextExpression = ParseNextExpression(tokens, context);

        if (nextExpression is not IValueGetter nextValueGetter)
        {
            throw new ScriptParsingException(
                source: currentToken,
                message: $"Expected a Value expression, but found: {nextExpression}");
        }

        return nextValueGetter;
    }

    public static IExecutable ParseNextExecutableExpression(
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        Token currentToken = tokens.Current;
        IExpression? nextExpression = ParseNextExpression(tokens, context);

        if (nextExpression is not IExecutable nextExecutable)
        {
            throw new ScriptParsingException(
                source: currentToken,
                message: $"Expected an executable expression, but found: {nextExpression}");
        }

        return nextExecutable;
    }


    public static IExpression? ParseNextExpression(
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        List<ParsingUnit> units = new List<ParsingUnit>();

        bool continueCollecting = true;

        do
        {
            switch (tokens.Current)
            {
                case SeparatorToken sepToken:
                    HandleNextSeparator(sepToken, ref continueCollecting, units, tokens, context);
                    break;

                case OperatorToken opToken:
                    HandleNextOperator(opToken, units, tokens, context);
                    break;

                case LiteralToken litToken:
                    HandleNextLiteral(litToken, units, tokens, context);
                    break;

                case IdentifierToken identToken:
                    HandleNextIdentifier(identToken, units, tokens, context);
                    break;

                case KeywordToken keywordToken:
                    HandleNextKeyword(keywordToken, units, tokens, context);
                    break;

                case EOFToken eofToken:
                    throw new ScriptParsingException(
                        source: eofToken,
                        message: $"Expression ended unexpectedly: {eofToken}");

                default:
                    HandleOtherToken(tokens.Current, units, tokens, context);
                    break;
            }
        }
        while (continueCollecting);

        //Now we have a list of Expression Units.
        //Reduce in the proper precedence order

        ReduceExpression(units);

        if (units.Count > 1)
        {
            throw new ScriptParsingException(
                source: units[1].FirstToken,
                message: $"Failed to reduce Expression to one value.");
        }

        if (units.Count == 0)
        {
            return null;
        }

        return units[0].AsExpression;
    }

    private static void HandleNextSeparator(
        SeparatorToken sepToken,
        ref bool continueCollecting,
        List<ParsingUnit> units,
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        switch (sepToken.separator)
        {
            case Separator.Semicolon:
            case Separator.Comma:
            case Separator.CloseParen:
            case Separator.CloseIndexer:
            case Separator.Colon:
            case Separator.CloseCurlyBoi:
                continueCollecting = false;
                break;

            case Separator.OpenIndexer:
                tokens.CautiousAdvance();
                IExpression? indexerExpression = ParseNextExpression(tokens, context);
                if (indexerExpression is not IValueGetter indexerValue)
                {
                    throw new ScriptParsingException(
                        source: sepToken,
                        message: $"Index operation requires value type: {indexerExpression}");
                }
                units.Add(new IndexAccessUnit(indexerValue, sepToken));
                tokens.AssertAndSkip(Separator.CloseIndexer);
                break;

            case Separator.OpenParen:
                tokens.CautiousAdvance();
                units.Add(new ParsedValuedUnit(ParseNextExpression(tokens, context), sepToken));
                tokens.AssertAndSkip(Separator.CloseParen);
                break;

            case Separator.OpenCurlyBoi:
                throw new ScriptParsingException(
                    source: sepToken,
                    message: $"CurlyBois cannot exist in an expression.  Are you missing a semicolon?: {sepToken}");

            default:
                throw new ScriptParsingException(
                    source: sepToken,
                    message: $"Unexpected Separator: {sepToken.separator}");
        }
    }

    private static void HandleNextOperator(
        OperatorToken opToken,
        List<ParsingUnit> units,
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        switch (opToken.operatorType)
        {
            case Operator.Assignment:
            case Operator.PlusEquals:
            case Operator.MinusEquals:
            case Operator.TimesEquals:
            case Operator.DivideEquals:
            case Operator.PowerEquals:
            case Operator.ModuloEquals:
            case Operator.AndEquals:
            case Operator.OrEquals:
            case Operator.Plus:
            case Operator.Minus:
            case Operator.Times:
            case Operator.Divide:
            case Operator.Power:
            case Operator.Modulo:
            case Operator.CastDouble:
            case Operator.CastInteger:
            case Operator.Increment:
            case Operator.Decrement:
            case Operator.Not:
            case Operator.IsEqualTo:
            case Operator.IsNotEqualTo:
            case Operator.IsGreaterThan:
            case Operator.IsGreaterThanOrEqualTo:
            case Operator.IsLessThan:
            case Operator.IsLessThanOrEqualTo:
            case Operator.And:
            case Operator.Or:
            case Operator.Negate:
                //Add and continue
                units.Add(new TokenUnit(opToken));
                tokens.CautiousAdvance();
                break;

            case Operator.Ternary:
                //Add the ternary Operator
                units.Add(new TokenUnit(opToken));
                tokens.CautiousAdvance();

                //Add the first expected expression
                Token firstToken = tokens.Current;
                units.Add(new ParsedValuedUnit(
                     value: ParseNextExpression(tokens, context),
                     firstToken: firstToken));

                //Colon
                tokens.AssertAndSkip(Separator.Colon);

                //Add the second expected expression
                firstToken = tokens.Current;
                units.Add(new ParsedValuedUnit(
                     value: ParseNextExpression(tokens, context),
                     firstToken: firstToken));
                break;

            case Operator.MemberAccess:
                tokens.CautiousAdvance();
                string identifier = tokens.GetIdentifierAndAdvance();
                Type[]? genericMethodArguments = tokens.TryReadTypeArguments();
                if (tokens.TestWithoutSkipping(Separator.OpenParen))
                {
                    //Method
                    units.Add(new MemberAccessUnit(
                        identifier: identifier,
                        args: ParseArguments(tokens, context),
                        genericMethodArguments: genericMethodArguments,
                        firstToken: opToken));
                }
                else
                {
                    if (genericMethodArguments is not null)
                    {
                        throw new ScriptParsingException(
                            source: opToken,
                            message: $"Unexpected GenericArguments in member context: {opToken.operatorType}");
                    }

                    //Value
                    units.Add(new MemberAccessUnit(
                        identifier: identifier,
                        args: null,
                        genericMethodArguments: null,
                        firstToken: opToken));
                }
                break;


            case Operator.AmbiguousMinus:
            default:
                throw new ScriptParsingException(
                    source: opToken,
                    message: $"Unsupported Operator: {opToken.operatorType}");
        }
    }

    private static void HandleNextLiteral(
        LiteralToken litToken,
        List<ParsingUnit> units,
        IEnumerator<Token> tokens,
        CompilationContext _)
    {
        units.Add(new ParsedValuedUnit(litToken, litToken));
        tokens.CautiousAdvance();
    }

    private static void HandleNextIdentifier(
        IdentifierToken identToken,
        List<ParsingUnit> units,
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        CompilationContext.IdentifierType type = context.GetIdentifierType(identToken.identifier);

        switch (type)
        {
            case CompilationContext.IdentifierType.Constant:
                tokens.CautiousAdvance();
                units.Add(new ParsedValuedUnit(
                    value: new ConstantToken(
                        source: identToken,
                        value: context.GetConstantValue(identToken.identifier)!,
                        valueType: context.GetConstantType(identToken.identifier)!),
                    firstToken: identToken));
                break;

            case CompilationContext.IdentifierType.Variable:
                tokens.CautiousAdvance();
                units.Add(new IdentifierUnit(identToken, context));
                break;

            case CompilationContext.IdentifierType.Function:
                tokens.CautiousAdvance();
                units.Add(new ParsedValuedUnit(
                    value: ParseFunctionCall(tokens, identToken, context),
                    firstToken: identToken));
                break;
        
            case CompilationContext.IdentifierType.Unidentified:
            default:
                throw new ScriptParsingException(
                    source: identToken,
                    message: $"Unidentified Identifier: {identToken.identifier} on line {identToken.line}");
        }

    }

    private static void HandleOtherToken(
        Token token,
        List<ParsingUnit> units,
        IEnumerator<Token> tokens,
        CompilationContext _)
    {
        units.Add(new TokenUnit(token));
        tokens.CautiousAdvance();
    }

    private static void HandleNextKeyword(
        KeywordToken keywordToken,
        List<ParsingUnit> units,
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        switch (keywordToken.keyword)
        {
            case Keyword.New:
                {
                    tokens.CautiousAdvance();
                    Type newObjectType = tokens.ReadTypeAndAdvance();
                    
                    if (tokens.TestWithoutSkipping(Separator.OpenIndexer) || newObjectType.IsArray)
                    {
                        //Array
                        IValueGetter? countValue = null;
                        IValueGetter[]? initializerValues = null;

                        if (tokens.TestAndConditionallySkip(Separator.OpenIndexer))
                        {
                            newObjectType = newObjectType.MakeArrayType();
                            IExpression? indexerExpression = ParseNextExpression(tokens, context);
                            countValue = indexerExpression as IValueGetter;

                            if (countValue is null)
                            {
                                throw new ScriptParsingException(
                                    source: tokens.Current,
                                    message: $"Index operation requires value type: {indexerExpression}");
                            }
                            tokens.AssertAndSkip(Separator.CloseIndexer);
                        }

                        if (tokens.TestWithoutSkipping(Separator.OpenCurlyBoi))
                        {
                            //Initializer List
                            Type? itemType = newObjectType.GetInitializerItemType();
                            if (itemType == null)
                            {
                                throw new ScriptParsingException(
                                    source: keywordToken,
                                    message: $"Initializer Lists only function on collections. " +
                                        $"Did you enter the wrong type, or possibly omit a semicolon at the end of the expression?");
                            }
                            //Initializer Syntax
                            initializerValues = ParseItems(tokens, itemType, context);
                        }

                        units.Add(new ParsedValuedUnit(
                            value: new ConstructArrayExpression(
                                arrayType: newObjectType,
                                arg: countValue,
                                initializer: initializerValues,
                                token: keywordToken),
                            firstToken: keywordToken));
                    }
                    else
                    {
                        //Non-Array

                        IValueGetter[] args = ParseArguments(tokens, context);

                        if (tokens.TestWithoutSkipping(Separator.OpenCurlyBoi))
                        {
                            Type? itemType = newObjectType.GetInitializerItemType();
                            if (itemType == null)
                            {
                                throw new ScriptParsingException(
                                    source: keywordToken,
                                    message: $"Initializer Lists only function on collections. " +
                                        $"Did you enter the wrong type, or possibly omit a semicolon at the end of the expression?");
                            }

                            //Initializer Syntax
                            IValueGetter[] items = ParseItems(tokens, itemType, context);

                            units.Add(new ParsedValuedUnit(
                                value: new ConstructInitializedCollectionExpression(
                                    objectType: newObjectType,
                                    args: args,
                                    items: items,
                                    source: keywordToken),
                                firstToken: keywordToken));
                        }
                        else
                        {
                            units.Add(new ParsedValuedUnit(
                                value: new ConstructObjectExpression(
                                    objectType: newObjectType,
                                    args: args),
                                firstToken: keywordToken));
                        }
                    }
                }
                break;

            default:
                units.Add(new TokenUnit(tokens.Current));
                tokens.CautiousAdvance();
                break;
        }
    }

    public static IValueGetter[] ParseArguments(
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        List<IValueGetter> arguments = new List<IValueGetter>();
        tokens.AssertAndSkip(Separator.OpenParen);

        if (!tokens.TestWithoutSkipping(Separator.CloseParen))
        {
            do
            {
                Token temp = tokens.Current;
                IExpression? nextExpression = ParseNextExpression(tokens, context);
                if (nextExpression is not IValueGetter nextValue)
                {
                    throw new ScriptParsingException(
                        source: temp,
                        message: $"Function Arguments must be values: {nextExpression}");
                }
                arguments.Add(nextValue);
            }
            while (tokens.TestAndConditionallySkip(Separator.Comma));
        }

        tokens.AssertAndSkip(Separator.CloseParen);

        return arguments.ToArray();
    }

    public static IValueGetter[] ParseItems(
        IEnumerator<Token> tokens,
        Type itemType,
        CompilationContext context)
    {
        List<IValueGetter> items = new List<IValueGetter>();
        tokens.AssertAndSkip(Separator.OpenCurlyBoi);

        if (!tokens.TestWithoutSkipping(Separator.CloseCurlyBoi))
        {
            do
            {
                Token temp = tokens.Current;
                IValueGetter nextValue = ParseNextGetterExpression(tokens, context);

                if (!itemType.AssignableFromType(nextValue.GetValueType()))
                {
                    throw new ScriptParsingException(
                        source: temp,
                        message: $"Initializer List Argument must be of appropriate value type: Expected: {itemType.Name}, Found: {nextValue.GetValueType().Name}");
                }

                items.Add(nextValue);
            }
            while (tokens.TestAndConditionallySkip(Separator.Comma));
        }

        tokens.AssertAndSkip(Separator.CloseCurlyBoi);

        return items.ToArray();
    }

    public static IExpression ParseFunctionCall(
        IEnumerator<Token> tokens,
        IdentifierToken identToken,
        CompilationContext context)
    {
        IValueGetter[] arguments = ParseArguments(tokens, context);

        string functionName = identToken.identifier;
        FunctionSignature functionSignature = context.GetFunctionSignature(functionName);

        if (arguments.Length != functionSignature.arguments.Length)
        {
            throw new ScriptParsingException(
                source: identToken,
                message: $"Incorrect number of arguments for function {functionName}.  " +
                    $"Expected: {functionSignature.arguments.Length}, Received: {arguments.Length}");
        }

        for (int i = 0; i < arguments.Length; i++)
        {
            if (!functionSignature.arguments[i].valueType.AssignableFromType(arguments[i].GetValueType()))
            {
                throw new ScriptParsingException(
                    source: identToken,
                    message: $"Incompatible type for argument {i+1} of function {functionName}.  " +
                        $"Expected: {functionSignature.arguments[i].valueType.Name}, " +
                        $"Received: {arguments[i].GetValueType().Name}");
            }
        }

        if (functionSignature.returnType == typeof(void))
        {
            return new FunctionExecutableOperation(
                operation: (RuntimeContext rtContext) =>
                    rtContext.RunVoidFunction(
                        functionName: functionName,
                        arguments: arguments.GetArgs(functionSignature, rtContext)));
        }
        else
        {
            return new FunctionValueOperation(
                outputType: functionSignature.returnType,
                operation: (RuntimeContext rtContext) =>
                    rtContext.RunFunction(
                        functionName: functionName,
                        arguments: arguments.GetArgs(functionSignature, rtContext)));
        }
    }

    private static void ReduceExpression(
        List<ParsingUnit> units)
    {
        ReduceMemberAccessAndIndexing(units);
        ReducePostFixes(units);
        ReducePreOperator(Operator.Negate, units);
        ReducePreFixes(units);
        ReducePreOperator(Operator.CastDouble, units);
        ReducePreOperator(Operator.CastInteger, units);

        ReduceBinaryOperator(Operator.Power, units);
        ReduceBinaryOperator(Operator.Times, units);
        ReduceBinaryOperator(Operator.Divide, units);
        ReduceBinaryOperator(Operator.Modulo, units);
        ReduceBinaryOperator(Operator.Plus, units);
        ReduceBinaryOperator(Operator.Minus, units);

        ReducePreOperator(Operator.Not, units);

        ReduceBinaryOperator(Operator.IsLessThan, units);
        ReduceBinaryOperator(Operator.IsGreaterThan, units);
        ReduceBinaryOperator(Operator.IsLessThanOrEqualTo, units);
        ReduceBinaryOperator(Operator.IsGreaterThanOrEqualTo, units);

        ReduceBinaryOperator(Operator.IsEqualTo, units);
        ReduceBinaryOperator(Operator.IsNotEqualTo, units);

        ReduceBinaryOperator(Operator.And, units);
        ReduceBinaryOperator(Operator.Or, units);

        ReduceTernaryOperator(units);

        ReduceAssignmentOperators(units);
    }

    private static void ReduceMemberAccessAndIndexing(List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] is MemberAccessUnit memberAccessUnit)
            {
                if (i == 0)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression began with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                //Remove
                units.RemoveAt(i);
                i--;

                if (units[i].AsValueGetter is null && units[i].AsType is not null)
                {
                    units[i] = new ParsedValuedUnit(
                        value: MemberManagement.HandleStaticExpression(
                            type: units[i].AsType!,
                            genericMethodArguments: memberAccessUnit.genericMethodArguments,
                            args: memberAccessUnit.args,
                            identifier: memberAccessUnit.identifier,
                            source: memberAccessUnit.FirstToken),
                        firstToken: units[i].FirstToken);
                }
                else
                {
                    //Swap the ParsingUnit for the calculated value
                    units[i] = new ParsedValuedUnit(
                        value: MemberManagement.HandleMemberExpression(
                            value: units[i].AsValueGetter!,
                            genericMethodArguments: memberAccessUnit.genericMethodArguments,
                            args: memberAccessUnit.args,
                            identifier: memberAccessUnit.identifier,
                            source: memberAccessUnit.FirstToken),
                        firstToken: units[i].FirstToken);
                }


            }
            else if (units[i] is IndexAccessUnit indexAccessUnit)
            {
                if (i == 0)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression began with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                //Remove
                units.RemoveAt(i);
                i--;

                //Swap the ParsingUnit for the calculated value
                units[i] = new ParsedValuedUnit(
                    value: new IndexerOperation(
                        containerArg: units[i].AsValueGetter!,
                        indexArg: indexAccessUnit.arg,
                        source: units[i].FirstToken),
                    firstToken: units[i].FirstToken);
            }
        }
    }

    private static void ReducePostFixes(List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            //It's a postfix operator if it follows a modifiable value
            if (units[i].OperatorType == Operator.Increment || units[i].OperatorType == Operator.Decrement)
            {
                if (i == 0 || units[i - 1].AsValue == null)
                {
                    //Definitely not a postfix
                    continue;
                }

                //Cache operatortoken and remove operator
                OperatorToken operatorToken = (units[i].FirstToken as OperatorToken)!;
                units.RemoveAt(i);

                //Move to Variable position
                i--;

                //Swap the ParsingUnit for the calculated value
                units[i] = new ParsedValuedUnit(
                    value: new UnaryValueOperation(
                        arg: units[i].AsValue!,
                        operatorToken: operatorToken,
                        prefix: false),
                    firstToken: units[i].FirstToken);
            }
        }
    }

    private static void ReducePreFixes(List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].OperatorType == Operator.Increment || units[i].OperatorType == Operator.Decrement)
            {
                if (i == units.Count - 1 || units[i + 1].AsValue == null)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Operator {units[i].OperatorType} could not be resolved as a Post-Fix or a Pre-Fix.  It can only be attached to a modifiable value: {units[i].FirstToken}");
                }

                //Cache Value and remove
                IValue value = units[i + 1].AsValue!;
                units.RemoveAt(i + 1);

                OperatorToken operatorToken = (units[i].FirstToken as OperatorToken)!;

                //Swap the ParsingUnit for the calculated value
                units[i] = new ParsedValuedUnit(
                    value: new UnaryValueOperation(
                        arg: value,
                        operatorToken: operatorToken,
                        prefix: true),
                    firstToken: units[i].FirstToken);
            }
        }
    }

    private static void ReducePreOperator(Operator op, List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].OperatorType == op)
            {
                if (i == units.Count - 1)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Unable to parse PreOperator {op}: No following value");
                }

                if (units[i + 1].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i + 1].FirstToken,
                        message: $"Unable to parse PreOperator {op}: Following value is not value not retrievable: {units[i + 1].FirstToken}");
                }

                //Cache Value and remove
                IValueGetter value = units[i + 1].AsValueGetter!;
                units.RemoveAt(i + 1);

                OperatorToken operatorToken = (units[i].FirstToken as OperatorToken)!;

                //Swap the ParsingUnit for the calculated value
                switch (op)
                {
                    case Operator.CastInteger:
                    case Operator.CastDouble:
                        units[i] = new ParsedValuedUnit(
                            value: CastOperation.CreateCastOperation(
                                arg: value,
                                operatorToken: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    case Operator.Negate:
                        units[i] = new ParsedValuedUnit(
                            value: NegationOperation.CreateNegationOperation(
                                arg: value,
                                source: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    case Operator.Not:
                        units[i] = new ParsedValuedUnit(
                            value: NotOperation.CreateNotOperation(
                                arg: value,
                                operatorToken: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    default:
                        throw new ArgumentException($"Unexpected Operator: {op}");
                }

            }
        }
    }

    private static void ReduceBinaryOperator(
        Operator op,
        List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].OperatorType == op)
            {
                if (i == 0)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression began with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                if (i == units.Count - 1)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression ended with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                if (units[i - 1].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i - 1].FirstToken,
                        message: $"Unexpected token before {units[i].OperatorType} operator: {units[i - 1].FirstToken}");
                }

                if (units[i + 1].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i + 1].FirstToken,
                        message: $"Unexpected token after {units[i].OperatorType} operator: {units[i + 1].FirstToken}");
                }

                OperatorToken operatorToken = (units[i].FirstToken as OperatorToken)!;

                //Cache Value and remove Right value and operator
                IValueGetter arg2Value = units[i + 1].AsValueGetter!;
                units.RemoveAt(i + 1);
                units.RemoveAt(i);

                //adjust our current position
                i--;

                //Swap the ParsingUnit for the calculated value

                if (HandleAsPrimitive(units[i].AsValueGetter!.GetValueType(), arg2Value.GetValueType()))
                {
                    switch (op)
                    {
                        case Operator.Plus:
                        case Operator.Minus:
                        case Operator.Times:
                        case Operator.Divide:
                        case Operator.Power:
                        case Operator.Modulo:
                            if ((units[i].AsValueGetter!.GetValueType() == typeof(string) ||
                                arg2Value.GetValueType() == typeof(string)) &&
                                op == Operator.Plus)
                            {
                                //Handle String Concatenation
                                units[i] = new ParsedValuedUnit(
                                    value: ConcatenateOperator.CreateConcatenateOperator(
                                        arg1: units[i].AsValueGetter!,
                                        arg2: arg2Value),
                                    firstToken: units[i].FirstToken);
                                break;
                            }
                            //Handle Numerical Operators
                            units[i] = new ParsedValuedUnit(
                                value: BinaryNumericalOperation.CreateBinaryNumericalOperation(
                                    arg1: units[i].AsValueGetter!,
                                    arg2: arg2Value,
                                    operatorToken: operatorToken),
                                firstToken: units[i].FirstToken);
                            break;

                        case Operator.IsEqualTo:
                        case Operator.IsNotEqualTo:
                            units[i] = new ParsedValuedUnit(
                                value: EqualityCompairsonOperation.CreateEqualityComparisonOperator(
                                    arg1: units[i].AsValueGetter!,
                                    arg2: arg2Value,
                                    operatorToken: operatorToken),
                                firstToken: units[i].FirstToken);
                            break;

                        case Operator.IsGreaterThan:
                        case Operator.IsGreaterThanOrEqualTo:
                        case Operator.IsLessThan:
                        case Operator.IsLessThanOrEqualTo:
                            units[i] = new ParsedValuedUnit(
                                value: ComparisonOperation.CreateComparisonOperation(
                                    arg1: units[i].AsValueGetter!,
                                    arg2: arg2Value,
                                    operatorToken: operatorToken),
                                firstToken: units[i].FirstToken);
                            break;

                        case Operator.And:
                        case Operator.Or:
                            units[i] = new ParsedValuedUnit(
                                value: BinaryBoolOperation.CreateBinaryBoolOperator(
                                    arg1: units[i].AsValueGetter!,
                                    arg2: arg2Value,
                                    operatorToken: operatorToken),
                                firstToken: units[i].FirstToken);
                            break;

                        default:
                            throw new ArgumentException($"Unexpected Operator: {op}");
                    }
                }
                else
                {
                    units[i] = new ParsedValuedUnit(
                        value: OverloadedOperation.CreateOverloadedOperator(
                            arg1: units[i].AsValueGetter!,
                            arg2: arg2Value,
                            operatorToken: operatorToken),
                        firstToken: units[i].FirstToken);
                }
            }
        }
    }

    private static void ReduceTernaryOperator(List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].OperatorType == Operator.Ternary)
            {
                if (i == 0)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression began with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                if (i >= units.Count - 2)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression with a {units[i].OperatorType} operator didn't contain enought arguments: {units[i].FirstToken}");
                }

                if (units[i - 1].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i - 1].FirstToken,
                        message: $"Unexpected token before {units[i].OperatorType} operator: {units[i - 1].FirstToken}");
                }

                if (units[i + 1].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i + 1].FirstToken,
                        message: $"Unexpected token after {units[i].OperatorType} operator: {units[i + 1].FirstToken}");
                }

                if (units[i + 2].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i + 1].FirstToken,
                        message: $"Unexpected token for second arguemnt of {units[i].OperatorType} operator: {units[i + 2].FirstToken}");
                }

                OperatorToken operatorToken = (units[i].FirstToken as OperatorToken)!;

                //Cache Value and remove Right value and operator
                IValueGetter arg1Value = units[i + 1].AsValueGetter!;
                IValueGetter arg2Value = units[i + 2].AsValueGetter!;
                units.RemoveAt(i + 2);
                units.RemoveAt(i + 1);
                units.RemoveAt(i);

                //adjust our current position
                i--;

                //Swap the ParsingUnit for the calculated value
                units[i] = new ParsedValuedUnit(
                    value: new TernaryOperation(
                        condition: units[i].AsValueGetter!,
                        arg1: arg1Value,
                        arg2: arg2Value,
                        operatorToken: operatorToken),
                    firstToken: units[i].FirstToken);
            }
        }
    }

    private static void ReduceAssignmentOperators(List<ParsingUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].OperatorType == Operator.Assignment ||
                units[i].OperatorType == Operator.PlusEquals ||
                units[i].OperatorType == Operator.MinusEquals ||
                units[i].OperatorType == Operator.TimesEquals ||
                units[i].OperatorType == Operator.DivideEquals ||
                units[i].OperatorType == Operator.PowerEquals ||
                units[i].OperatorType == Operator.ModuloEquals ||
                units[i].OperatorType == Operator.AndEquals ||
                units[i].OperatorType == Operator.OrEquals)
            {
                if (i == 0)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression began with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                if (i == units.Count - 1)
                {
                    throw new ScriptParsingException(
                        source: units[i].FirstToken,
                        message: $"Expression ended with a {units[i].OperatorType} operator: {units[i].FirstToken}");
                }

                if (units[i - 1].AsValue == null)
                {
                    throw new ScriptParsingException(
                        source: units[i - 1].FirstToken,
                        message: $"Expression before {units[i].OperatorType} operator must be modifiable: {units[i - 1].FirstToken}");
                }

                if (units[i + 1].AsValueGetter == null)
                {
                    throw new ScriptParsingException(
                        source: units[i + 1].FirstToken,
                        message: $"Expression after {units[i].OperatorType} operator must have a value: {units[i + 1].FirstToken}");
                }

                OperatorToken operatorToken = (units[i].FirstToken as OperatorToken)!;

                //Cache Value and remove Right value and operator
                IValueGetter value = units[i + 1].AsValueGetter!;
                units.RemoveAt(i + 1);
                units.RemoveAt(i);

                //adjust our current position
                i--;

                //Swap the ParsingUnit for the calculated value

                switch (operatorToken.operatorType)
                {
                    case Operator.Assignment:
                        units[i] = new ParsedValuedUnit(
                            value: new AssignmentOperation(
                                assignee: units[i].AsValue!,
                                value: value,
                                source: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    case Operator.PlusEquals:
                        units[i] = new ParsedValuedUnit(
                            value: new PlusEqualsOperation(
                                assignee: units[i].AsValue!,
                                value: value,
                                source: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    case Operator.MinusEquals:
                    case Operator.TimesEquals:
                    case Operator.DivideEquals:
                    case Operator.PowerEquals:
                    case Operator.ModuloEquals:
                        //Handle Numerical Operators
                        units[i] = new ParsedValuedUnit(
                            value: new NumericalInPlaceOperation(
                                assignee: units[i].AsValue!,
                                value: value,
                                operatorType: operatorToken.operatorType,
                                source: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    case Operator.AndEquals:
                    case Operator.OrEquals:
                        units[i] = new ParsedValuedUnit(
                            value: new BooleanInPlaceOperation(
                                assignee: units[i].AsValue!,
                                value: value,
                                operatorType: operatorToken.operatorType,
                                source: operatorToken),
                            firstToken: units[i].FirstToken);
                        break;

                    default:
                        throw new ArgumentException($"Unexpected Operator: {operatorToken.operatorType}");
                }
            }
        }
    }

    private abstract class ParsingUnit
    {
        public virtual Operator OperatorType => Operator.MAX;
        public virtual IExpression? AsExpression => null;
        public virtual IValueSetter? AsValueSetter => null;
        public virtual IValueGetter? AsValueGetter => null;
        public virtual IValue? AsValue => null;
        public virtual IExecutable? AsExecutable => null;
        public virtual Type? AsType => null;

        public abstract Token FirstToken { get; }

    }

    private class TokenUnit : ParsingUnit
    {
        public readonly Token token;
        public override Operator OperatorType { get; }
        public override Type? AsType { get; } = null;
        public override Token FirstToken => token;

        public TokenUnit(Token token)
        {
            this.token = token;

            if (token is OperatorToken opToken)
            {
                OperatorType = opToken.operatorType;
            }
            else if (token is TypeToken typeToken)
            {
                AsType = typeToken.type;
            }
            else
            {
                OperatorType = Operator.MAX;
            }
        }
    }

    private class IdentifierUnit : ParsingUnit
    {
        public override IValue AsValue { get; }
        public override IValueSetter AsValueSetter => AsValue;
        public override IValueGetter AsValueGetter => AsValue;
        public override IExpression AsExpression => AsValue;
        public override Token FirstToken { get; }

        public IdentifierUnit(
            IdentifierToken token,
            CompilationContext context)
        {
            FirstToken = token;

            AsValue = new IdentifierExpression(
                identifierToken: token,
                context: context);
        }
    }

    private class ParsedValuedUnit : ParsingUnit
    {
        public override IExpression? AsExpression { get; }
        public override IExecutable? AsExecutable => AsExpression as IExecutable;
        public override IValueGetter? AsValueGetter => AsExpression as IValueGetter;
        public override IValueSetter? AsValueSetter => AsExpression as IValueSetter;
        public override IValue? AsValue => AsExpression as IValue;
        public override Token FirstToken { get; }

        public ParsedValuedUnit(
            IExpression? value,
            Token firstToken)
        {
            AsExpression = value;
            FirstToken = firstToken;
        }
    }

    private class MemberAccessUnit : ParsingUnit
    {
        public override Token FirstToken { get; }
        public override Operator OperatorType => Operator.MemberAccess;

        public readonly string identifier;
        public readonly IValueGetter[]? args;
        public readonly Type[]? genericMethodArguments;

        public MemberAccessUnit(
            string identifier,
            IValueGetter[]? args,
            Type[]? genericMethodArguments,
            Token firstToken)
        {
            this.identifier = identifier;
            this.args = args;
            this.genericMethodArguments = genericMethodArguments;
            FirstToken = firstToken;
        }
    }

    private class IndexAccessUnit : ParsingUnit
    {
        public override Token FirstToken { get; }
        public override Operator OperatorType => Operator.Indexing;

        public readonly IValueGetter arg;

        public IndexAccessUnit(
            IValueGetter arg,
            Token firstToken)
        {
            this.arg = arg;
            FirstToken = firstToken;
        }
    }

    private static bool HandleAsPrimitive(
        Type type1,
        Type type2)
    {
        return ((type1.IsPrimitive || type1 == typeof(string) || type1 == typeof(decimal)) &&
            (type2.IsPrimitive || type2 == typeof(string) || type2 == typeof(decimal)));
    }
}
