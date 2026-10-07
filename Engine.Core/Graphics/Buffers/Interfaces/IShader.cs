namespace PBG.Graphics;

public interface IShader
{
    public string GetPath();
    public bool RemoveDescriptorSet(Descriptor descriptor);
}