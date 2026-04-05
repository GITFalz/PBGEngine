namespace PBG.Debug;

public class PrintCounter
{
    private int _counter = 0;
    public void Print(string identifier = "")
    {
        Console.WriteLine(identifier + " " + _counter++);
    }
}