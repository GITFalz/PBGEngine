using PBG.Graphics;
using PBG.MathLibrary;

public class SimpleModelMesh
{
    //public static ShaderProgram RigShader = new ShaderProgram("model/model.vert", "model/model.frag");
    public SimpleModel Model;

    /*
    // Mesh
    private VAO _vao = new VAO();
    private IBO _ibo = new IBO();
    private VBO<Vector3> _vertVbo = new(new List<Vector3>());
    private VBO<Vector2> _uvVbo = new(new List<Vector2>());
    private VBO<Vector2i> _textureVbo = new(new List<Vector2i>());
    private VBO<Vector3> _normalVbo = new(new List<Vector3>());
    */
    private int _indexCount = 0;
    
    public SimpleModelMesh(SimpleModel model) 
    {
        Model = model;
    }
    
    public void GenerateMesh(List<Vector3> transformedVerts, List<Vector2> uvs, List<Vector2i> textureIndices, List<Vector3> normals)
    {
        List<uint> indices = [];
        for (uint i = 0; i < transformedVerts.Count; i++)
        {
            indices.Add(i);
        }
        _indexCount = indices.Count;

        /*
        _vertVbo.Renew(transformedVerts);
        _uvVbo.Renew(uvs);
        _textureVbo.Renew(textureIndices);
        _normalVbo.Renew(normals);
        _ibo.Renew(indices);

        _vao.Bind();
        
        _vao.LinkToVAO(0, 3, VertexAttribPointerType.Float, 0, 0, _vertVbo);
        _vao.LinkToVAO(1, 2, VertexAttribPointerType.Float, 0, 0, _uvVbo);
        _vao.IntLinkToVAO(2, 2, VertexAttribIntegerType.Int, 0, 0, _textureVbo);
        _vao.LinkToVAO(3, 3, VertexAttribPointerType.Float, 0, 0, _normalVbo); 

        _vao.Unbind();  
        */
    }

    public void Delete()
    {
        /*
        // Mesh
        _vao.DeleteBuffer();
        _ibo.DeleteBuffer();
        _vertVbo.DeleteBuffer();
        _uvVbo.DeleteBuffer();
        _textureVbo.DeleteBuffer();
        _normalVbo.DeleteBuffer();
        */
    }

    public void Render()
    {
        /*
        _vao.Bind();
        _ibo.Bind();

        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);

        _vao.Unbind();
        _ibo.Unbind();
        */
    }
}