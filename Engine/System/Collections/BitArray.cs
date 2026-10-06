using System.Collections;
using PBG.Core;
using PBG.MathLibrary;

namespace PBG.Collections;

public unsafe class BitArray : IEnumerable<ulong>, IDisposable
{
    private ulong* _bits;
    private int _count;
    private int _knownSingleSlotIndex = 0;

    public int Length => _count;
    public readonly int Slots;
    public bool IsEmpty => OneCount() == 0;

    public BitArray(int count)
    {
        Slots = (count + 63) >> 6;
        _bits = MemoryHelper.Alloc<ulong>(Slots);
        _count = count;
    }

    public ulong this[int index]
    {
        get => GetInt(index);
        set => Set(index, value);
    }

    /// <summary>
    /// If you know what you are doing, has no bounds check
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ulong GetUnsafe(int index) => (_bits[index >> 6] >> (index & 0x3F)) & 1ul;

    public int GetEmptySlotIndex()
    {
        int total = _knownSingleSlotIndex * 64;
        int remaining = _count - total;

        for (int i = _knownSingleSlotIndex; i < Slots && remaining > 0; i++)
        {
            ulong bits = _bits[i];
            int bitsInThisWord = Math.Min(64, remaining);

            if (bitsInThisWord < 64)
                bits &= (1ul << bitsInThisWord) - 1;

            int trailingOnes = Bit.TrailingOnes(bits);
            total += trailingOnes;

            if (trailingOnes < bitsInThisWord)
            {
                bits |= 1ul << trailingOnes;
                _knownSingleSlotIndex = bits < ulong.MaxValue ? i : 0;
                _bits[i] = bits;
                return total;
            }

            remaining -= bitsInThisWord;
        }

        return -1;
    }

    public ulong GetInt(int index)
    {
        BoundsCheck(index);

        int outer = index >> 6;
        int inner = index & 0x3F;

        return (_bits[outer] >> inner) & 1ul;
    }

    public bool Set(int index, ulong state)
    {
        BoundsCheck(index);

        state &= 1;

        int outer = index >> 6;
        int inner = index & 0x3F;

        ulong v = _bits[outer];
        ulong old = (v >> inner) & 1ul;
        _bits[outer] = (v & ~(1ul << inner)) | (state << inner);

        if (state == 0 && outer < _knownSingleSlotIndex)
            _knownSingleSlotIndex = outer;

        return old != state;
    }

    public bool Set(int index)
    {
        BoundsCheck(index);

        int outer = index >> 6;
        int inner = index & 0x3F;

        ulong old = (_bits[outer] >> inner) & 1ul;
        _bits[outer] |= 1ul << inner;

        return old == 0ul;
    }
    
    public void SetMap(int index, int size)
    {
        if (size <= 0) return;
        BoundsCheck(index, size);

        int outer = index >> 6;
        int inner = index & 0x3F;
        int firstBits = Math.Min(size, 64 - inner);

        _bits[outer] |= Bit.TrailingOnes(firstBits) << inner;

        size -= firstBits;
        outer++;

        while (size >= 64)
        {
            _bits[outer++] = ulong.MaxValue;
            size -= 64;
        }

        if (size > 0) _bits[outer] |= Bit.TrailingOnes(size);
    }

    public void RemoveMap(int index, int size)
    {
        if (size <= 0) return;
        BoundsCheck(index, size);

        int outer = index >> 6;
        int inner = index & 0x3F;
        int firstBits = Math.Min(size, 64 - inner);

        _bits[outer] &= ~(Bit.TrailingOnes(firstBits) << inner);

        if (outer < _knownSingleSlotIndex)
            _knownSingleSlotIndex = outer;

        size -= firstBits;
        outer++;

        while (size >= 64)
        {
            _bits[outer++] = 0;
            size -= 64;
        }

        if (size > 0) _bits[outer] &= ~Bit.TrailingOnes(size);
    }

    public bool Remove(int index)
    {
        BoundsCheck(index);

        int outer = index >> 6;
        int inner = index & 0x3F;

        ulong old = (_bits[outer] >> inner) & 1ul;
        _bits[outer] &= ~(1ul << inner);

        if (outer < _knownSingleSlotIndex)
            _knownSingleSlotIndex = outer;

        return old == 1ul;
    }

    public int OneCount()
    {
        int count = 0;
        for (int i = 0; i < Slots; i++)
            count += System.Numerics.BitOperations.PopCount(_bits[i]);
        return count;
    }

    private void BoundsCheck(int index)
    {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException($"trying to index outside of bounds of bit array ({index} out of bounds of {_count})");
    }

    private void BoundsCheck(int index, int size)
    {
        if (index < 0 || size < 0 || index + size > _count)
            throw new IndexOutOfRangeException($"trying to index outside of bounds of bit array ({index} and size {size} out of bounds of {_count})");
    }

    IEnumerator<ulong> IEnumerable<ulong>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Enumerator GetEnumerator() => new(this);

    public void Clear()
    {
        MemoryHelper.Clear(_bits, Slots);
        _knownSingleSlotIndex =  0;
    }

    public void Dispose()
    {
        MemoryHelper.Free(_bits);
        _knownSingleSlotIndex =  0;
        GC.SuppressFinalize(this);
    }

    public struct Enumerator : IEnumerator<ulong>
    {
        private readonly ulong* _bits;
        private readonly int _count;
        private int _index;

        internal Enumerator(BitArray array)
        {
            _bits = array._bits;
            _count = array._count;
            _index = -1;

            //Console.WriteLine("Created enumerator: " + _bits.Length + " " + _count + " " + _index);
        }

        public readonly ulong Current
        {
            get
            {
                int outer = _index >> 6;
                int inner = _index & 0x3F;
                return (_bits[outer] >> inner) & 1ul;
            }
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        readonly object IEnumerator.Current => Current;

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}