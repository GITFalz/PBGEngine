namespace PBG.Asset;

public class AssetLoader<T> where T : class
{
    public Queue<T> Datas = [];
    public void Add(T data) => Datas.Enqueue(data);
    public void Clear() => Datas.Clear();
}