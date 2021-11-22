using System.Runtime.CompilerServices;

namespace BGC;

public static class Debug
{
    //private static Action<string> logCallback = null;

    public delegate void LogHandler(string message);
    public delegate void ExceptionHandler(Exception excp, string filePath, string memberName, int lineNumber);

    public static event ExceptionHandler? ExceptionCallback;
    public static event LogHandler? LogCallback;
    public static event LogHandler? LogWarningCallback;
    public static event LogHandler? LogErrorCallback;

    public static void Log(string message)
    {
        if (LogCallback is not null)
        {
            LogCallback(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    public static void LogWarning(string message)
    {
        if (LogWarningCallback is not null)
        {
            LogWarningCallback(message);
        }
        else if (LogCallback is not null)
        {
            LogCallback($"WARNING: {message}");
        }
        else
        {
            Console.WriteLine($"WARNING: {message}");
        }
    }

    public static void LogException(
        Exception excp,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (ExceptionCallback is not null)
        {
            ExceptionCallback(excp, filePath, memberName, lineNumber);
        }
        else if (LogErrorCallback is not null)
        {
            LogErrorCallback($"EXCEPTION @{filePath}:{lineNumber} in member {memberName}: {excp}");
        }
        else if (LogCallback is not null)
        {
            LogCallback($"EXCEPTION @{filePath}:{lineNumber} in member {memberName}: {excp}");
        }
        else
        {
            Console.WriteLine($"EXCEPTION @{filePath}:{lineNumber} in member {memberName}: {excp}");
        }
    }

    public static void LogError(string message)
    {
        if (LogErrorCallback is not null)
        {
            LogErrorCallback(message);
        }
        else if (LogCallback is not null)
        {
            LogCallback($"ERROR: {message}");
        }
        else
        {
            Console.WriteLine($"ERROR: {message}");
        }
    }

    public static void Assert(bool condition)
    {
#if DEBUG
            if (!condition)
            {
                throw new Exception($"Assertion failed!");
            }
#endif
    }

    public static void Assert(bool condition, string message)
    {
#if DEBUG
            if (!condition)
            {
                throw new Exception($"Assertion failed with message: {message}");
            }
#endif
    }

}
