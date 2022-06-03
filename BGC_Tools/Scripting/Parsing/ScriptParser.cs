namespace BGC.Scripting;

public static class ScriptParser
{
    /// <summary>
    /// Parses the script string and ensures the expected functions are present.
    /// </summary>
    /// <exception cref="ScriptParsingException"></exception>
    public static Script LexAndParseScript(
        string script,
        params FunctionSignature[] expectedFunctions)
    {
        Script scriptObject;
        using (GeneralScriptReader reader = new GeneralScriptReader(script))
        {
            IEnumerator<Token> tokens = reader
                .GetTokens()
                .ExpandInterpolatedStrings()
                .DropComments()
                .HandleArrays()
                .HandleElseIf()
                .HandleAmbiguousMinus()
                .HandleCasting()
                .CheckParens()
                .GetEnumerator();

            tokens.MoveNext();

            scriptObject = new Script(script, tokens, expectedFunctions);
        }

        return scriptObject;
    }

    private static IEnumerable<Token> ExpandInterpolatedStrings(this IEnumerable<Token> tokens)
    {
        foreach (Token token in tokens)
        {
            if (token is InterpolatedString interpolatedString)
            {
                foreach (Token rewrittenToken in interpolatedString.RewriteToken().ExpandInterpolatedStrings())
                {
                    yield return rewrittenToken;
                }
            }
            else
            {
                yield return token;
            }
        }
    }

    //Check that all parens are matched
    private static IEnumerable<Token> CheckParens(this IEnumerable<Token> tokens)
    {
        //var temp = tokens.ToList();
        Stack<Separator> invocationStack = new Stack<Separator>();

        foreach (Token token in tokens)
        {
            if (token is SeparatorToken separator)
            {
                switch (separator.separator)
                {
                    case Separator.OpenCurlyBoi:
                        invocationStack.Push(Separator.CloseCurlyBoi);
                        break;

                    case Separator.OpenIndexer:
                        invocationStack.Push(Separator.CloseIndexer);
                        break;

                    case Separator.OpenParen:
                        invocationStack.Push(Separator.CloseParen);
                        break;

                    case Separator.CloseCurlyBoi:
                    case Separator.CloseIndexer:
                    case Separator.CloseParen:
                        //Check for unmatched
                        if (invocationStack.Count == 0)
                        {
                            throw new ScriptParsingException(
                                source: separator,
                                message: $"Unmatched CloseParen: {separator.separator}");
                        }

                        Separator sep = invocationStack.Pop();
                        //Check closing type
                        if (sep != separator.separator)
                        {
                            throw new ScriptParsingException(
                                source: separator,
                                message: $"Unexpected separator {separator.separator}.  Expected separator: {sep}");
                        }
                        break;

                    default:
                        break;
                }
            }

            yield return token;
        }

        if (invocationStack.Count != 0)
        {
            throw new ScriptParsingException(
                source: tokens.LastOrDefault() ?? new EOFToken(0, 0),
                message: $"Mismatched Parentheses found!");
        }
    }

    private static IEnumerable<Token> HandleCasting(this IEnumerable<Token> tokens)
    {
        //Handle conversion of ( double ) to cast operation
        Operator operatorType = Operator.CastDouble;
        Queue<Token> tokenQueue = new Queue<Token>(3);

        foreach (Token token in tokens)
        {
            switch (tokenQueue.Count)
            {
                case 0:
                    if (token is SeparatorToken sep && sep.separator == Separator.OpenParen)
                    {
                        tokenQueue.Enqueue(token);
                    }
                    else
                    {
                        yield return token;
                    }
                    break;

                case 1:
                    if (token is TypeToken typeToken && (typeToken.type == typeof(int) || typeToken.type == typeof(double)))
                    {
                        tokenQueue.Enqueue(token);

                        if (typeToken.type == typeof(int))
                        {
                            operatorType = Operator.CastInteger;
                        }
                        else
                        {
                            operatorType = Operator.CastDouble;
                        }
                    }
                    else
                    {
                        yield return tokenQueue.Dequeue();
                        yield return token;
                    }
                    break;

                case 2:
                    if (token is SeparatorToken sep2 && sep2.separator == Separator.CloseParen)
                    {
                        tokenQueue.Clear();
                        yield return new OperatorToken(token, operatorType);
                    }
                    else
                    {
                        yield return tokenQueue.Dequeue();
                        yield return tokenQueue.Dequeue();
                        yield return token;
                    }
                    break;

                default:
                    throw new Exception($"Serious parsing error.  Too many queued tokens.");
            }
        }

        while (tokenQueue.Count > 0)
        {
            yield return tokenQueue.Dequeue();
        }
    }

    /// <summary>
    /// Replaces AmbiguousMinus, AmbiguousLessThan, and AmbiguousGreaterThan
    /// </summary>
    private static IEnumerable<Token> HandleAmbiguousMinus(this IEnumerable<Token> tokens)
    {
        Token? priorToken = null;

        foreach (Token token in tokens)
        {
            if (token is OperatorToken op && op.operatorType == Operator.AmbiguousMinus)
            {
                if (priorToken is OperatorToken ||
                    (priorToken is SeparatorToken sep &&
                     (sep.separator == Separator.OpenParen || sep.separator == Separator.Comma)))
                {
                    yield return new OperatorToken(token, Operator.Negate);
                }
                else
                {
                    yield return new OperatorToken(token, Operator.Minus);
                }
            }
            else
            {
                yield return token;
            }

            priorToken = token;
        }
    }

    /// <summary>
    /// Replaces AmbiguousMinus, AmbiguousLessThan, and AmbiguousGreaterThan
    /// </summary>
    private static IEnumerable<Token> HandleArrays(this IEnumerable<Token> tokens)
    {
        TypeToken? priorTypeToken = null;
        SeparatorToken? openBracketToken = null;

        foreach (Token token in tokens)
        {
            if (openBracketToken is not null)
            {
                //Accumulated "Type["
                if (token is SeparatorToken closeBracketToken && closeBracketToken.separator == Separator.CloseIndexer)
                {
                    //stashing "Type[]"
                    priorTypeToken = new TypeToken(
                        source: priorTypeToken!,
                        alias: $"{priorTypeToken!.alias}[]",
                        type: priorTypeToken.type.MakeArrayType());

                    openBracketToken = null;
                    continue;
                }
                else
                {
                    //Output "Type" and "["
                    yield return priorTypeToken!;
                    priorTypeToken = null;

                    yield return openBracketToken;
                    openBracketToken = null;

                    //Continue on in case token is a type
                }
            }
            
            if (priorTypeToken is not null)
            {
                //Accumulated "Type"
                if (token is SeparatorToken sepToken && sepToken.separator == Separator.OpenIndexer)
                {
                    //stashing "["
                    openBracketToken = sepToken;

                    continue;
                }
                else
                {
                    //Output "Type" and "["
                    yield return priorTypeToken!;
                    priorTypeToken = null;

                    //Continue on in case token is a type
                }
            }
            
            if (token is TypeToken typeToken)
            {
                priorTypeToken = typeToken;
                continue;
            }
            else
            {
                yield return token;
            }
        }

        if (priorTypeToken is not null)
        {
            yield return priorTypeToken;
        }

        if (openBracketToken is not null)
        {
            yield return openBracketToken;
        }
    }

    private static IEnumerable<Token> HandleElseIf(this IEnumerable<Token> tokens)
    {
        Token? stashedToken = null;

        foreach (Token token in tokens)
        {
            if (stashedToken is null)
            {
                //First stage of identification - Find Else
                if (token is KeywordToken kw && kw.keyword == Keyword.Else)
                {
                    //Hold it and don't yield it
                    stashedToken = token;
                }
                else
                {
                    yield return token;
                }
            }
            else
            {
                //Second stage of identification - Find If
                if (token is KeywordToken kwn && kwn.keyword == Keyword.If)
                {
                    yield return new KeywordToken(token, Keyword.ElseIf);
                }
                else
                {
                    //Not Else If: Return each of these
                    yield return stashedToken;
                    yield return token;
                }

                //Clear stashed token
                stashedToken = null;
            }
        }

        //return stashedToken
        if (stashedToken is not null)
        {
            yield return stashedToken;
        }
    }

    private static IEnumerable<Token> DropComments(this IEnumerable<Token> tokens) =>
        tokens.Where(x => x is not CommentToken);
}
