namespace BGC.Scripting;

public abstract class Statement : IExecutable
{
    public abstract FlowState Execute(ScopeRuntimeContext context, CancellationToken ct);

    public static IExecutable? ParseNextStatement(
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        Token leadingToken = tokens.Current;

        switch (leadingToken)
        {
            case SeparatorToken sepToken:
                //Valid operations:
                //  Just a semicolon (empty statement)
                //  OpenCurlyBoi (new block)
                switch (sepToken.separator)
                {
                    case Separator.Semicolon:
                        //Empty statement.  Kick out.
                        //Skip checking for EOF because it's possible for the last statement to be
                        //  just a semicolon.  This would trigger the EOF exception
                        tokens.CautiousAdvance(checkEOF: false);
                        return null;

                    case Separator.OpenCurlyBoi:
                        tokens.CautiousAdvance();
                        IExecutable statement = new Block(tokens, context);
                        tokens.AssertAndSkip(Separator.CloseCurlyBoi, checkEOF: false);
                        return statement;

                    default:
                        throw new ScriptParsingException(
                            source: sepToken,
                            message: $"Statement cannot begin with a {sepToken.separator}.");
                }


            case KeywordToken kwToken:
                //Valid operations:
                //  Continue, Break
                //  Return (With or Without return value)
                //  Declaration (With or Without assignment, with or without const)
                //  If ( Condition )
                //  While ( Condition )
                //  For ( A; B; C )
                return ParseKeywordStatement(kwToken, tokens, context);

            case TypeToken typeToken:
                //Valid operations:
                //  Declaration (With or Without assignment, with or without const)
                return ParseTypeStatement(typeToken, tokens, context);

            case LiteralToken _:
            case IdentifierToken _:
            case OperatorToken _:
                //Valid operations:
                //  Assignment
                //  PostIncrement
                //  PreIncrement
                IExecutable standardExecutable = Expression.ParseNextExecutableExpression(tokens, context);
                tokens.AssertAndSkip(Separator.Semicolon, checkEOF: false);
                return standardExecutable;

            default:
                throw new Exception($"Unexpected TokenType: {leadingToken}");
        }
    }

    private static IExecutable? ParseKeywordStatement(
        KeywordToken kwToken,
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        //Valid operations:
        //  Continue, Break
        //  Return (With or Without return value)
        //  Declaration (With or Without assignment, with or without global)
        //  If ( Condition )
        //  While ( Condition )
        //  For ( A; B; C )

        switch (kwToken.keyword)
        {
            case Keyword.If:
                {
                    tokens.CautiousAdvance();
                    tokens.AssertAndSkip(Separator.OpenParen);
                    IValueGetter ifTest = Expression.ParseNextGetterExpression(tokens, context);
                    tokens.AssertAndSkip(Separator.CloseParen);

                    IExecutable trueStatement = ParseNextStatement(tokens, context)!;
                    IExecutable? falseStatement = null;

                    if (trueStatement == null)
                    {
                        throw new ScriptParsingException(
                            source: kwToken,
                            message: $"No statement returned for If block: {kwToken}");
                    }

                    //Check the next token for Else or Else IF
                    if (tokens.TestAndConditionallySkip(Keyword.Else))
                    {
                        falseStatement = ParseNextStatement(tokens, context);
                    }
                    else if (tokens.TestWithoutSkipping(Keyword.ElseIf))
                    {
                        KeywordToken replacementIf = new KeywordToken(
                            source: tokens.Current,
                            keyword: Keyword.If);

                        falseStatement = ParseKeywordStatement(
                            kwToken: replacementIf,
                            tokens: tokens,
                            context: context);
                    }

                    return new IfStatement(
                        condition: ifTest,
                        trueBlock: trueStatement,
                        falseBlock: falseStatement,
                        keywordToken: kwToken);
                }

            case Keyword.Switch:
                {
                    tokens.CautiousAdvance();
                    tokens.AssertAndSkip(Separator.OpenParen);
                    IValueGetter switchExpression = Expression.ParseNextGetterExpression(tokens, context);
                    tokens.AssertAndSkip(Separator.CloseParen);

                    return ParseSwitch(
                        tokens: tokens,
                        switchExpression: switchExpression,
                        context: context,
                        keywordToken: kwToken);
                }

            case Keyword.While:
                {
                    tokens.CautiousAdvance();
                    //New context is used for loop
                    context = context.CreateChildScope(true);

                    tokens.AssertAndSkip(Separator.OpenParen);
                    IValueGetter conditionTest = Expression.ParseNextGetterExpression(tokens, context);
                    tokens.AssertAndSkip(Separator.CloseParen);

                    IExecutable? loopBody = ParseNextStatement(tokens, context);

                    return new WhileStatement(
                        continueExpression: conditionTest,
                        loopBody: loopBody,
                        keywordToken: kwToken);
                }

            case Keyword.For:
                {
                    tokens.CautiousAdvance();
                    //New context is used for loop
                    context = context.CreateChildScope(true);

                    //Open Paren
                    tokens.AssertAndSkip(Separator.OpenParen);

                    //Initialization
                    IExecutable? initializationStatement = ParseNextStatement(tokens, context);

                    //Semicolon already skipped by statement parsing

                    //Continue Expression
                    IValueGetter continueExpression = Expression.ParseNextGetterExpression(tokens, context);

                    //Semicolon
                    tokens.AssertAndSkip(Separator.Semicolon);

                    //Increment
                    IExecutable? incrementStatement = ParseForIncrementer(tokens, context);

                    //Close Paren
                    tokens.AssertAndSkip(Separator.CloseParen);

                    IExecutable? loopBody = ParseNextStatement(tokens, context);

                    return new ForStatement(
                        initializationStatement: initializationStatement,
                        continueExpression: continueExpression,
                        incrementStatement: incrementStatement,
                        loopBody: loopBody,
                        keywordToken: kwToken);
                }

            case Keyword.ForEach:
                {
                    tokens.CautiousAdvance();

                    //New context is used for loop
                    context = context.CreateChildScope(true);

                    //Open Paren
                    tokens.AssertAndSkip(Separator.OpenParen);

                    //Item Declaration
                    Type itemType = tokens.ReadTypeAndAdvance();
                    IdentifierToken identifierToken = tokens.GetTokenAndAdvance<IdentifierToken>();

                    IExecutable declaration = new DeclarationOperation(
                        identifierToken: identifierToken,
                        valueType: itemType,
                        context: context);

                    IValue loopVariable = new IdentifierExpression(
                        identifierToken: identifierToken,
                        context: context);

                    tokens.AssertAndSkip(Keyword.In);

                    //Container
                    IValueGetter containerExpression = Expression.ParseNextGetterExpression(tokens, context);

                    //Close Paren
                    tokens.AssertAndSkip(Separator.CloseParen);

                    IExecutable? loopBody = ParseNextStatement(tokens, context);

                    return new ForEachStatement(
                        declarationStatement: declaration,
                        loopVariable: loopVariable,
                        containerExpression: containerExpression,
                        loopBody: loopBody,
                        keywordToken: kwToken);
                }

            case Keyword.Continue:
            case Keyword.Break:
                {
                    tokens.CautiousAdvance();
                    tokens.AssertAndSkip(Separator.Semicolon, false);
                    return new ControlStatement(kwToken, context);
                }

            case Keyword.Return:
                {
                    tokens.CautiousAdvance();
                    IExecutable returnStatement = new ReturnStatement(
                        keywordToken: kwToken,
                        returnValue: Expression.ParseNextGetterExpression(tokens, context),
                        context: context);
                    tokens.AssertAndSkip(Separator.Semicolon, false);
                    return returnStatement;
                }

            case Keyword.Extern:
            case Keyword.Global:
                throw new ScriptParsingException(
                    source: kwToken,
                    message: $"{kwToken.keyword} variable declarations invalid in local context.  Put them outside classes and methods.");


            case Keyword.Const:
                tokens.CautiousAdvance();
                if (tokens.Current is TypeToken typeToken)
                {
                    return ParseTypeStatement(
                        typeToken: typeToken,
                        tokens: tokens,
                        context: context,
                        constDeclaration: true);
                }
                throw new ScriptParsingException(
                    source: kwToken,
                    message: $"The Const keyword can only appear at the start of a declaration: {kwToken}");

            case Keyword.ElseIf:
            case Keyword.Else:
                throw new ScriptParsingException(
                    source: kwToken,
                    message: $"Unpaired {kwToken.keyword} token: {kwToken}");

            default:
                throw new ScriptParsingException(
                    source: kwToken,
                    message: $"A Statement cannot begin with this keyword: {kwToken}");
        }

    }

    private static IExecutable? ParseTypeStatement(
        TypeToken typeToken,
        IEnumerator<Token> tokens,
        CompilationContext context,
        bool constDeclaration = false)
    {
        //Valid operations:
        //  Declaration (With or Without assignment, with or without global)

        Type valueType = tokens.ReadTypeAndAdvance();

        IdentifierToken identToken = tokens.GetTokenAndAdvance<IdentifierToken>();

        if (tokens.TestAndConditionallySkip(Separator.Semicolon, false))
        {
            if (constDeclaration)
            {
                throw new ScriptParsingException(
                    source: typeToken,
                    message: $"const variable declared without a value.  What is the point?");
            }

            return new DeclarationOperation(
                identifierToken: identToken,
                valueType: valueType,
                context: context);
        }
        else if (tokens.TestAndConditionallySkip(Operator.Assignment))
        {
            IValueGetter initializerExpression = Expression.ParseNextGetterExpression(tokens, context);

            tokens.AssertAndSkip(Separator.Semicolon, false);

            return DeclarationAssignmentOperation.CreateDelcaration(
                identifierToken: identToken,
                valueType: valueType,
                initializer: initializerExpression,
                isConstant: constDeclaration,
                context: context);
        }

        throw new ScriptParsingException(
            source: identToken,
            message: $"Invalid variable declaration: {typeToken} {identToken} {tokens.Current}");

    }

    public static IExecutable? ParseForIncrementer(
        IEnumerator<Token> tokens,
        CompilationContext context)
    {
        if (tokens.TestWithoutSkipping(Separator.CloseParen))
        {
            return null;
        }

        List<IExecutable> returnStatements = new List<IExecutable>();

        do
        {
            returnStatements.Add(Expression.ParseNextExecutableExpression(tokens, context));
        }
        while (tokens.TestAndConditionallySkip(Separator.Comma));

        if (returnStatements.Count == 1)
        {
            return returnStatements[0];
        }

        return new MultiStatement(returnStatements);
    }

    public static SwitchStatement ParseSwitch(
        IEnumerator<Token> tokens,
        IValueGetter switchExpression,
        CompilationContext context,
        KeywordToken keywordToken)
    {
        tokens.AssertAndSkip(Separator.OpenCurlyBoi);
        Type switchType = switchExpression.GetValueType();

        Dictionary<object, Block> map = new Dictionary<object, Block>();
        Block? defaultBlock = null;

        //Parse all blocks
        while (true)
        {
            List<object> mappedValues = new List<object>();
            bool defaultInPlay = false;

            //Parse Labels
            while (true)
            {
                if (tokens.TestAndConditionallySkip(Keyword.Case))
                {
                    LiteralToken switchStatementValue = tokens.GetTokenAndAdvance<LiteralToken>();
                    tokens.AssertAndSkip(Separator.Colon);

                    if (!switchStatementValue.GetValueType().IsAssignableTo(switchType))
                    {
                        throw new ScriptParsingException(tokens.Current, $"Unable to assign switch case value {switchStatementValue} of type {switchStatementValue.GetValueType()} to switch value type {switchType}");
                    }

                    mappedValues.Add(switchStatementValue.GetAs<object>());

                    continue;
                }
                else if (tokens.TestAndConditionallySkip(Keyword.Default))
                {
                    tokens.AssertAndSkip(Separator.Colon);

                    if (defaultInPlay || defaultBlock is not null)
                    {
                        throw new ScriptParsingException(tokens.Current, $"Multiple instances of default found for switch statement.");
                    }

                    defaultInPlay = true;
                    continue;
                }

                break;
            }

            if (mappedValues.Count == 0 && !defaultInPlay)
            {
                throw new ScriptParsingException(tokens.Current, $"Entered Switch condition body without condition labels.");
            }

            //Parse Block
            Block switchBlock = new Block(tokens, context);

            //Assign default
            if (defaultInPlay)
            {
                defaultBlock = switchBlock;
            }

            //Assign to labels
            foreach (object value in mappedValues)
            {
                if (map.ContainsKey(value))
                {
                    throw new ScriptParsingException(tokens.Current, $"Multiple instances of case {value} found in switch statement.");
                }

                map[value] = switchBlock;
            }

            if (tokens.TestAndConditionallySkip(Separator.CloseCurlyBoi, false))
            {
                break;
            }
        }

        return new SwitchStatement(
            switchValue: switchExpression,
            defaultBlock: defaultBlock,
            switchBlocks: map,
            keywordToken: keywordToken);
    }
}
