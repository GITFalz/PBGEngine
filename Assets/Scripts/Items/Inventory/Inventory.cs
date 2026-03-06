using PBG.Graphics;
using PBG.MathLibrary;
public class Inventory
{
    //public static ShaderProgram Shader = new ShaderProgram("Inventory/Items/Items.vert", "Utils/ArrayImages.frag");

    //public static int ModelLocation = Shader.GetLocation("model");
    //public static int ProjectionLocation = Shader.GetLocation("projection");
    //public static int TextureLocation = Shader.GetLocation("textureArray");

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public Item?[] Items { get; private set; }

    public Inventory(uint width, uint height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        Items = new Item?[Width * Height];
    }
}