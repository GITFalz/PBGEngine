using System.Collections;

namespace PBG.Data;

public class HashList<T>
{
    private HashSet<T> _hash = [];
    private List<T> _elements = [];

    public bool Add(T t)
    {
        if (!_hash.Add(t))
            return false;

        _elements.Add(t);
        return true;
    }

    public bool Remove(T t)
    {
        if (!_hash.Remove(t))
            return false;

        _elements.Remove(t);
        return true;
    }
}