namespace PBG.Asset;

public class Asset<T> where T : class
{
    public T? Data = null;
    public Asset() {}
    public Asset(T? data) 
    { 
        Data = data; 
    }
    public static implicit operator T?(Asset<T> handler) => handler.Data;
}