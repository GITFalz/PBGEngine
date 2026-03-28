namespace PBG.Data;

public class StaticStarter
{
    public bool _started = false;

    public void Run(Action action)
    {
        if (_started)
            return;

        _started = true;

        action.Invoke();
    }
}