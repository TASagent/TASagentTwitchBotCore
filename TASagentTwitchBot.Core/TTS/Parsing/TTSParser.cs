using System.Text;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.TTS.Parsing.Tokens;
using TASagentTwitchBot.Core.TTS.Parsing.RenderElements;

namespace TASagentTwitchBot.Core.TTS.Parsing;

public static class TTSParser
{
    public static async Task<AudioRequest?> ParseTTS(
        string text,
        TTSSystemRenderer renderer,
        ISoundEffectSystem soundEffectSystem)
    {
        using TTSReader reader = new TTSReader(text);

        IEnumerable<RenderElement> renderElements = reader
            .GetTokens()
            .GlueStrings()
            .HandleSoundEffects(soundEffectSystem)
            .GlueStrings()
            .ToList()
            .HandleCommands()
            .HandleRenderModeModifiers()
            .StripRemainingMarkup()
            .GlueStrings()
            .StripUnnecessaryWhitespace()
            .ToRenderElements();

        return await renderer.Render(renderElements);
    }

    public static async Task<(string?, int)> ParseTTSNoSoundEffects(
        string text,
        TTSSystemRenderer renderer)
    {
        using TTSReader reader = new TTSReader(text);

        IEnumerable<RenderElement> renderElements = reader
            .GetTokens()
            .StripSpecials()
            .GlueStrings()
            .ToList()
            .HandleRenderModeModifiers()
            .StripRemainingMarkup()
            .GlueStrings()
            .StripUnnecessaryWhitespace()
            .ToRenderElements();

        return await renderer.RenderRaw(renderElements);
    }

    private static IEnumerable<ParsingUnit> HandleSoundEffects(
        this IEnumerable<ParsingUnit> tokens,
        ISoundEffectSystem soundEffectSystem)
    {
        foreach (ParsingUnit token in tokens)
        {
            if (token is ApparentSoundEffectToken apparentSoundEffect)
            {
                SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByAlias(apparentSoundEffect.soundEffect);

                if (soundEffect is not null)
                {
                    //Return parsed SoundEffect
                    yield return new SoundEffectUnit(apparentSoundEffect.position, soundEffect);
                }
                else
                {
                    //Return constitutient components as string
                    yield return new StringUnit(apparentSoundEffect.position, apparentSoundEffect.ToString());
                }
            }
            else
            {
                yield return token;
            }
        }
    }

    private static IEnumerable<ParsingUnit> StripSpecials(this IEnumerable<ParsingUnit> tokens)
    {
        foreach (ParsingUnit token in tokens)
        {
            if (token is ApparentSoundEffectToken apparentSoundEffect)
            {
                //Re-stringify
                yield return new StringUnit(apparentSoundEffect.position, apparentSoundEffect.ToString());
            }
            else if (token is ApparentCommandToken apparentCommand)
            {
                //Re-stringify
                yield return new StringUnit(apparentCommand.position, apparentCommand.ToString());
            }
            else
            {
                yield return token;
            }
        }
    }

    private static List<ParsingUnit> HandleCommands(this List<ParsingUnit> parsingUnitList)
    {
        for (int i = 0; i < parsingUnitList.Count; i++)
        {
            if (parsingUnitList[i] is ApparentCommandToken apparentCommand)
            {
                if (!CommandUnit.ParseAndSubstitute(parsingUnitList, i))
                {
                    parsingUnitList[i] = new StringUnit(apparentCommand.position, apparentCommand.ToString());
                }
            }
        }

        return parsingUnitList;
    }

    private static List<ParsingUnit> HandleRenderModeModifiers(this List<ParsingUnit> parsingUnitList)
    {
        for (int i = 0; i < parsingUnitList.Count; i++)
        {
            if (parsingUnitList[i] is MarkupToken markupToken)
            {
                switch (markupToken.markup)
                {
                    case TTSMarkup.Underscore:
                    case TTSMarkup.Asterisk:
                        {
                            int nextIndex = parsingUnitList.FindNextMarkup(i, markupToken.markup);

                            if (nextIndex == -1)
                            {
                                //No matching pair found
                                goto case TTSMarkup.MAX;
                            }

                            parsingUnitList[i] = new RenderModeModifier(markupToken.position, TTSRenderMode.Emphasis, true);
                            parsingUnitList[nextIndex] = new RenderModeModifier(parsingUnitList[nextIndex].position, TTSRenderMode.Emphasis, false);
                        }
                        break;

                    case TTSMarkup.OpenParen:
                        {
                            int nextIndex = parsingUnitList.FindNextMarkup(i, TTSMarkup.CloseParen);

                            if (nextIndex == -1)
                            {
                                //No matching pair found
                                goto case TTSMarkup.MAX;
                            }

                            parsingUnitList[i] = new RenderModeModifier(markupToken.position, TTSRenderMode.Whisper, true);
                            parsingUnitList[nextIndex] = new RenderModeModifier(parsingUnitList[nextIndex].position, TTSRenderMode.Whisper, false);
                        }
                        break;

                    case TTSMarkup.CloseParen:
                        //No remaining use for CloseParen
                        goto case TTSMarkup.MAX;

                    case TTSMarkup.Tilde:
                        {
                            int nextIndex = parsingUnitList.FindNextMarkup(i, TTSMarkup.Tilde);

                            if (nextIndex == -1)
                            {
                                //No matching pair found
                                goto case TTSMarkup.MAX;
                            }

                            parsingUnitList[i] = new RenderModeModifier(markupToken.position, TTSRenderMode.Censor, true);
                            parsingUnitList[nextIndex] = new RenderModeModifier(parsingUnitList[nextIndex].position, TTSRenderMode.Censor, false);
                        }
                        break;

                    case TTSMarkup.MAX:
                        //Discard token
                        parsingUnitList[i] = new StringUnit(markupToken.position, markupToken.ToString());
                        break;

                    default:
                        break;
                }
            }
        }

        return parsingUnitList;
    }

    private static IEnumerable<ParsingUnit> StripRemainingMarkup(this IEnumerable<ParsingUnit> parsingUnits)
    {
        foreach (ParsingUnit parsingUnit in parsingUnits)
        {
            if (parsingUnit is MarkupToken)
            {
                yield return new StringUnit(parsingUnit.position, parsingUnit.ToString()!);
            }
            else
            {
                yield return parsingUnit;
            }
        }
    }

    /// <summary>
    /// Simplifies the stream by concatenating adjacent strings, whitespace, and words
    /// </summary>
    private static IEnumerable<ParsingUnit> GlueStrings(this IEnumerable<ParsingUnit> tokens)
    {
        StringBuilder stringBuilder = new StringBuilder();
        Queue<ParsingUnit> tokenQueue = new Queue<ParsingUnit>();

        foreach (ParsingUnit token in tokens)
        {
            if (token is StringUnit)
            {
                //Accumulate words
                tokenQueue.Enqueue(token);
                continue;
            }

            //Non-String - Deplete Queue
            if (tokenQueue.Count == 0)
            {
                //Empty Queue
                yield return token;
            }
            else if (tokenQueue.Count == 1)
            {
                //Single Item Queue
                //Return
                yield return tokenQueue.Dequeue();
                yield return token;
            }
            else
            {
                //Multi-Item Queue
                //Consolidate
                ParsingUnit queueNext = tokenQueue.Dequeue();
                int position = queueNext.position;
                stringBuilder.Clear();
                stringBuilder.Append(queueNext.ToString());

                while (tokenQueue.Count > 0)
                {
                    queueNext = tokenQueue.Dequeue();
                    stringBuilder.Append(queueNext.ToString());
                }

                yield return new StringUnit(position, stringBuilder.ToString());
                yield return token;
            }
        }
    }

    private static IEnumerable<ParsingUnit> StripUnnecessaryWhitespace(this IEnumerable<ParsingUnit> parsingUnits)
    {
        foreach (ParsingUnit parsingUnit in parsingUnits)
        {
            if (parsingUnit is StringUnit stringUnit && string.IsNullOrWhiteSpace(stringUnit.text))
            {
                continue;
            }

            yield return parsingUnit;
        }
    }

    private static IEnumerable<RenderElement> ToRenderElements(this IEnumerable<ParsingUnit> parsingUnits)
    {
        TTSRenderMode renderMode = TTSRenderMode.Normal;

        foreach (ParsingUnit unit in parsingUnits)
        {
            switch (unit)
            {
                case PauseCommandUnit pauseCommand:
                    yield return new SoundElement(new AudioDelay(pauseCommand.duration));
                    break;

                case SoundEffectUnit soundEffect:
                    yield return new SoundElement(new SoundEffectRequest(soundEffect.soundEffect));
                    break;

                case RenderModeModifier renderModeModifier:
                    if (renderModeModifier.enable)
                    {
                        renderMode |= renderModeModifier.renderMode;
                    }
                    else
                    {
                        renderMode &= TTSRenderMode.MASK & ~renderModeModifier.renderMode;
                    }
                    break;

                case StringUnit stringUnit:
                    yield return new TextElement(stringUnit.text, renderMode);
                    break;

                case SentinelToken:
                    break;

                default:
                    throw new NotImplementedException($"ParsingUnit support not implemented: {unit.GetType().Name} - {unit}");
            }
        }
    }

    private static int FindNextMarkup(this List<ParsingUnit> parsingUnitList, int index, TTSMarkup markup)
    {
        for (int i = index + 1; i < parsingUnitList.Count; i++)
        {
            if (parsingUnitList[i] is MarkupToken markupToken && markupToken.markup == markup)
            {
                return i;
            }
        }

        return -1;
    }
}
