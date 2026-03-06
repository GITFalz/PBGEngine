using PBG;
using PBG.Graphics;
using PBG.Threads;
using PBG.Voxel;

public class LODWorldGenerator
{
    /*
    public static ComputeShader? LodComputeShader = null;
    private static int _chunkWorldPosition;
    private static int _chunkBlockLevel;

    public static ComputeShader _lodMeshComputeShader = new ComputeShader("computeShaders/lod/lodMesh.comp");
    private static int _chunkMeshWorldPosition = _lodMeshComputeShader.GetLocation("uChunkWorldPosition");

    public static AtomicCounter _atomicCounter = new();
    public static SSBO<int> BlockSSBO = new(new int[32768]);
    public static SSBO<uint> OcclusionMaskSSBO = new(new uint[192]);
    */

    public static void ReloadBlockComputeShader(int precompiledComputeShaderID)
    {
        /*
        if (LodComputeShader == null)
            LodComputeShader = new ComputeShader("computeShaders/lod/lodBlock_test.comp", precompiledComputeShaderID);
        else    
            LodComputeShader.RenewWithPreComp("computeShaders/lod/lodBlock_test.comp", precompiledComputeShaderID);

        _chunkWorldPosition = LodComputeShader.GetLocation("uChunkWorldPosition");
        _chunkBlockLevel = LodComputeShader.GetLocation("uLevel");
        */
    }
    
    public static void ClearBlockSSBO()
    {
        int[] data = new int[32768];
        for (int i = 0; i < 32768; i++)
            data[i] = -1;
        //BlockSSBO.Update(data, 0);
        //OcclusionMaskSSBO.Update(new uint[192], 0);
    }

    LODChunk? _currentChunk = null;
    uint _currentCount = 0;

    public void Generate(LODVoxelRenderer renderer)
    {
        if (renderer.GenerationQueue.TryDequeue(out var chunk))
        {
            LODWorldGenerationProcess process = new(renderer, chunk);
            TaskPool.QueueAction(process, TaskPriority.Low);
        }

        /*
        if (LodComputeShader == null)
            return;

        if (_currentChunk == null)
        {
            if (!renderer.GenerationQueue.TryDequeue(out var chunk))
                return;

            chunk.ChunkVertexVBO?.DeleteBuffer();
            chunk.HasBlocks = false;

            _atomicCounter.Update(0, 0);
            ClearBlockSSBO();

            LodComputeShader.Bind();

            GL.Uniform3(_chunkWorldPosition, ref chunk.WorldPosition);
            GL.Uniform1(_chunkBlockLevel, chunk.Level);
            
            _atomicCounter.Bind(0);
            BlockSSBO.Bind(1);
            OcclusionMaskSSBO.Bind(2);
            
            LodComputeShader.DispatchComputeBarrier(4, 1, 4);

            LodComputeShader.Unbind();

            _currentCount = _atomicCounter.ReadData();
            if (_currentCount == 0)
            {
                _currentChunk = null;
                return;
            }

            _currentChunk = chunk;
        }
        else
        {
            BlockVertexData[] vertexArray = new BlockVertexData[_currentCount*4];
            uint[] indicesArray = new uint[_currentCount*6];

            _currentChunk.ChunkVertexVBO = new(vertexArray);
            _currentChunk.ChunkIBO = new(indicesArray);

            _currentChunk.BindBuffers();

            _atomicCounter.Update(0, 0);

            _lodMeshComputeShader.Bind();

            GL.Uniform3(_chunkMeshWorldPosition, ref _currentChunk.WorldPosition);
            
            _atomicCounter.Bind(0);
            BlockSSBO.Bind(1);
            OcclusionMaskSSBO.Bind(2);
            Shader.BufferBind(_currentChunk.ChunkVertexVBO.ID, 3);
            Shader.BufferBind(_currentChunk.ChunkIBO.ID, 4);
            BlockData.BlockFaceSSBO.Bind(5);
            
            _lodMeshComputeShader.DispatchComputeBarrier(4, 1, 3);

            _lodMeshComputeShader.Unbind();

            _currentChunk.HasBlocks = true;
            _currentChunk.VertexCount = (int)_currentCount * 6;
            renderer.Chunks.Add(_currentChunk);

            _currentChunk = null;
        }
        */
    }
}