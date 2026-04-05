namespace PBG.MathLibrary;

public interface IVector<T>
{
    public int Count { get; }
    public IVector<T> Default { get; }
    public T this[int index] { get; set; }
}