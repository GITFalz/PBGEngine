using Compiler;
using PBG.MathLibrary;
using PBG.Assets.Scripts.NoiseNodes;
using PBG.Compiler.Lines;
using PBG.MathLibrary;

public abstract class BaseStructureGenerator
{
    public bool Debug = false;

    public abstract void ResetScore();
    public abstract float GetScore();
    public abstract int GetHeight(int x, int z);
}

public class DefaultStructureGenerator : BaseStructureGenerator
{
    public override void ResetScore() => StructureCompiler.ResetScore();
    public override float GetScore() => StructureCompiler.GetScore();
    public override int GetHeight(int x, int z)
    {
        Vector2 position = (x + 0.001f, z + 0.001f) * new Vector2(0.01f, 0.01f);
        float result = (PBG.Noise.NoiseLib.Noise(position) + 1) * 0.5f;
        return Mathf.Lerp(0, 60, result).Fti();
    }
}

public class WorldStructureGenerator : BaseStructureGenerator
{
    public Variable ScoreVariable;
    public NoiseNodeManager HeightManager;

    public WorldStructureGenerator(Variable scoreValue, NoiseNodeManager heightManager)
    {
        ScoreVariable = scoreValue;
        HeightManager = heightManager;
    }
    
    public override void ResetScore() => ScoreVariable.Result.SetFloat(1);
    public override float GetScore() => ScoreVariable.Result.GetFloat();
    public override int GetHeight(int x, int z)
    {
        HeightManager.Basic(x, z);
        return StructureManager.GetHeight(HeightManager);
    }
}