using PBG.MathLibrary;

namespace PBG.NewVoxel;

public unsafe static class GreedyMesher2
{
    public static void HandleGreedyFront(Block* blocks, List<Vector4i> vertexData, ulong* bitMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[z + (i + 1) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[z + (i + 1 + w) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(0, trailingZeros | (i << 5) | (z << 10), 0, h | (w << 5)));
            }
        }
    }

    public static void HandleGreedyBack(List<Vector4i> vertexData, ulong* bitMap, uint* data, int z)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[z + 2 + (i + 1) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[z + 2 + (i + 1 + w) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(20, trailingZeros | (i << 5) | (z << 10), 0, h | (w << 5)));
            }
        }
    }


    public static void HandleGreedyRight(List<Vector4i> vertexData, uint* bitMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint front = ~bitMap[i];
            uint row = data[i] & front;

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    uint sFront = ~bitMap[i];
                    uint sRow = data[i + w] & sFront;

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(4, x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyLeft(List<Vector4i> vertexData, uint* bitMap, uint* data, int x)
    {
        for (int i = 0; i < 32; i++)
        {
            uint front = ~bitMap[i];
            uint row = data[i] & front;

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    uint sFront = ~bitMap[i];
                    uint sRow = data[i + w] & sFront;

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(12, x | (i << 5) | (trailingZeros << 10), 0, (w << 5) | (h << 10)));
            }
        }
    }

    public static void HandleGreedyTop(List<Vector4i> vertexData, ulong* bitMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i + 1 + (y + 2) * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    ulong sFront = ~bitMap[i + w + 1 + (y + 2) * 34];
                    uint sRow = data[i + w] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[i + w] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(8, trailingZeros | (y << 5) | (i << 10), 0, h | (w << 10) ));
            }
        }
    }

    public static void HandleGreedyBottom(List<Vector4i> vertexData, ulong* bitMap, uint* data, int y)
    {
        for (int i = 0; i < 32; i++)
        {
            ulong front = ~bitMap[i + 1 + y * 34];
            uint row = data[i] & (uint)(front >> 1);

            while (row != 0)
            {
                int trailingZeros = Bit.TrailingZeros(row);

                uint newRow = row >> trailingZeros;

                int h = Bit.TrailingZeros(~newRow);

                uint mask = (uint)((1UL << h) - 1) << trailingZeros;

                row &= ~mask;

                int w = 1;

                while (w < 32 - i)
                {
                    int iw = i + w;
                    ulong sFront = ~bitMap[iw + 1 + y * 34];
                    uint sRow = data[iw] & (uint)(sFront >> 1);

                    if ((sRow & mask) != mask)
                        break;

                    data[iw] &= ~mask;
                    w++;
                }

                w--;
                h--;

                vertexData.Add(new(16, trailingZeros | (y << 5) | (i << 10), 0, h | (w << 10)));
            }
        }
    }
}