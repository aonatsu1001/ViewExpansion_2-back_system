using System;

public static class Clock
{
    public static long NowMs()
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}
