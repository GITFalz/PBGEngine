public interface IVector<T>
{
    public uint ElementCount { get; }
    public T this[int index] { get; set; }
}