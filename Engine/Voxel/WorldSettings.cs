using PBG.MathLibrary;

public static class WorldSettings
{
    public static float Time;
    public static double ElapsedWorldTime;
    public static float DaySpeed = 600;
    public static bool Paused = false;
    public static bool ShowChunkDebug = false;

    public static void Tick(float deltaTime)
    {
        if (Paused)
            return;

        ElapsedWorldTime += deltaTime;
        Time = Mathf.Fraction((float)(ElapsedWorldTime / DaySpeed));
    }

    public static void SetTime(float seconds)
    {
        ElapsedWorldTime = seconds;
        Time = Mathf.Fraction((float)(ElapsedWorldTime / DaySpeed));
    }

    public static void AddTime(float seconds)
    {
        ElapsedWorldTime += seconds;
        Time = Mathf.Fraction((float)(ElapsedWorldTime / DaySpeed));
    }

    public static void SetDaySpeed(float speed)
    {
        if (speed == 0)
        {
            Pause();
            return;
        }

        ElapsedWorldTime /= DaySpeed / speed;
        DaySpeed = speed;
    }

    public static void Pause() => Paused = true;
    public static void Resume()
    {
        if (DaySpeed == 0)
            return;

        Paused = false;
    }
}