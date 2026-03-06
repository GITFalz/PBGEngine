using System.Linq.Expressions;
using PBG.MathLibrary;
using PBG.MathLibrary;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes;

public class PositionNode(GetterValue[] getters, SetterValue[] setters, string type) : NoiseNode(getters, setters, type)
{
    public SetterValue Position = setters[0];
    
    public override void Basic(int x, int y) => Function(x, y);
    public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y) => Function(x, y);
    public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y) => Function(x, y);

    public void Function(int x, int y)
    {
        var value = new Vector2i(x, y);
        //Position.SetValue(value);
    }

    protected override Expression BuildBasicExpression() => BuildExpression();
    protected override Expression BuildTerrainExpression() => BuildExpression();
    private Expression BuildExpression()
    {
        return Const(0);
    }
}