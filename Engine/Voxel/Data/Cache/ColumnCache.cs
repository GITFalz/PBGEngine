
using PBG.MathLibrary;

public class ColumnCache(Vector3i key)
{
    public Vector3i Key = key;
    public Vector3i WorldPosition;
    public Vector3[] Data = new Vector3[32 * 32];

    public volatile bool IsReady = false;

    public int Initializing; // 0 = no, 1 = yes

    public ManualResetEventSlim WaitHandle = new(false);
    public int Priority = 1;

    public Vector3 Get(int x, int y)
    {
        int index = y + x * 32;
        return Data[index];
    }

    public void Set(int x, int y, Vector3 value)
    {
        int index = y + x * 32;
        Data[index] = value;
    }


}