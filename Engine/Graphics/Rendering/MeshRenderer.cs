using PBG.Asset;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;

namespace PBG.Rendering;

public class MeshRenderer : ScriptingNode
{
    public Descriptor Descriptor = Material.DefaultMaterial.GetDescriptor();

    [Field]
    public Mesh? Mesh
    {
        get => _mesh;
        set
        {
            if (_mesh == value)
                return;

            if (_mesh != null && !_mesh.IsCached)
                _mesh.Dispose();
                
            _mesh = value;
        }
    }
    private Mesh? _mesh = new();
    
    public void Render()
    {   
        _mesh?.Render(this);
    }

    public void Dispose()
    {
        Descriptor.Dispose();
        if (_mesh != null && !_mesh.IsCached)
            _mesh.Dispose();
    }
}