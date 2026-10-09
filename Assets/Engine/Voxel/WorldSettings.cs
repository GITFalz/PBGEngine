using PBG.MathLibrary;

public static class WorldSettings
{
    public static float Time;
    public static double ElapsedWorldTime;
    public static double DaySpeed = 60;
    public static bool Paused = false;
    public static bool ShowChunkDebug = false;

    public static void Tick(double deltaTime)
    {
        if (Paused)
            return;

        ElapsedWorldTime += deltaTime;
        Time = Mathf.Fraction((float)(ElapsedWorldTime / DaySpeed));
    }

    public static void SetTime(double seconds)
    {
        ElapsedWorldTime = seconds;
        Time = Mathf.Fraction((float)(ElapsedWorldTime / DaySpeed));
    }

    public static void AddTime(double seconds)
    {
        ElapsedWorldTime += seconds;
        Time = Mathf.Fraction((float)(ElapsedWorldTime / DaySpeed));
    }

    public static void SetDaySpeed(double speed)
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

    public static void SetRunning(bool running)
    {
        if (!running && DaySpeed == 0)
            return;

        Paused = !running;
    }
}