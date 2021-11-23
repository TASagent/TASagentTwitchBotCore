namespace BGC.Utility;

public static class Text
{
    public static string AsPlacement(this int value)
    {
        if (value < 0)
        {
            return $"-{AsPlacement(-1 * value)}";
        }

        //Isolate the Teens of every magnitude because they're exceptions to the otherwise simple rules.
        if (value % 100 is >= 10 and < 20)
        {
            return $"{value}th";
        }

        switch (value % 10)
        {
            case 1: return $"{value}st";
            case 2: return $"{value}nd";
            case 3: return $"{value}rd";
            default: return $"{value}th";
        }
    }
}
