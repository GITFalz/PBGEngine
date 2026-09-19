using System.Diagnostics;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.NewVoxel;
using PBG.Nodes;
using PBG.UI;
using Newtonsoft.Json;

public class WorldNodeEditor : ScriptingNode
{
    private PBGNodes _nodes = null!;

    private UIController _editorUI = null!;
    
    private VoxelRenderer _renderer = null!;

    private static UIText _fpsText = null!;
    private static UIText _positionText = null!;
    private static UIText _vertexCountText = null!;

    private static UIText _chunkPositionText = null!;
    private static UIText _chunkStatusText = null!;
    private static UIText _allocationsCountText = null!;
    
    private static UIText _globalChunkSpeed = null!;
    private static UIText _generationChunkSpeed = null!;
    private static UIText _renderingChunkSpeed = null!;
    private static UIText _uploadingChunkSpeed = null!;

    private static UIText _generationQueueCountText = null!;
    private static UIText _renderingQueueCountText = null!;
    private static UIText _uploadingQueueCountText = null!;
    private static UIText _dataPoolQueueCountText = null!;
    private static UIText _dictionnaryCountText = null!;
    private static UIText _cacheCountText = null!;

    private static UIText _managedRamUsageText = null!;
    private static UIText _ramUsageText = null!;
    private static UIText _aliveChunksText = null!;

    private static UIText VramTotalText = null!;
    private static UIText VramFreeText = null!;
    private static UIText VramUsedText = null!;
    private static UIText VramPrecentText = null!;

    private static UIText _averageChunkGenText = null!;
    private static UIText _averageChunkReadText = null!;
    private static UIText _averageFrameUploadTimeText = null!;

    private static UIText[] _generationThreadUsages = null!;
    private static UIText[] _renderingThreadUsages = null!;

    private static UIGraph _frameTimeGraph = null!;

    private Stopwatch _sw = Stopwatch.StartNew();
    private double _time = 0;
    

    private string _baseWorldComputeShaderPath = Game.ShaderPath / "computeShaders" / "world_vulkan" / "newWorldBase.comp";
    private string _finalWorldComputeShaderPath = Game.ShaderPath / "computeShaders" / "world_vulkan" / "newWorldFinal.comp";



    public static RollingAverageDoubleTimer ChunkGenerationTimer = new(2000);
    public static RollingAverageDoubleTimer ChunkRenderingTimer = new(2000);
    public static RollingAverageDoubleTimer TotalUploadTimer = new(2000);

    public static int GlobalCount = 0;

    public static int GenerationCount = 0;
    public static int RenderingCount = 0;
    public static int UploadCount = 0;


    private WorldEditorSettings _settings;
    

    void Start()
    {
        _nodes = Transform.Scene.QueryComponent<PBGNodes>();
        _editorUI = Transform.GetComponent<UIController>();
        _renderer = Transform.Scene.QueryComponent<VoxelRenderer>();
        
        _generationThreadUsages = new UIText[VoxelRenderer.GenerationThreads];
        _renderingThreadUsages = new UIText[VoxelRenderer.RenderingThreads];



        string settingsPath = Game.SettingsPath / "world_editor_settings.json";

        if (!File.Exists(settingsPath))
        {
            _settings = new()
            {
                CameraPositionY = 280,

                CameraFrontX = 0.696f,
                CameraFrontY = -0.179f,
                CameraFrontZ = 0.696f,

                CameraSpeed = 75,
                DaySpeed = 600,
                DayTime = 150,

                AmbientOcclusion = true,
                DayNightCycle = false,
                RenderNormal = true,
                RenderWireframe = true,
            };
        }
        else
        {
            JsonSerializerSettings jsonSettings = new()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var json = File.ReadAllText(settingsPath);
            _settings = JsonConvert.DeserializeObject<WorldEditorSettings>(json, jsonSettings);
        }

        _renderer.Camera.Position = (_settings.CameraPositionX, _settings.CameraPositionY, _settings.CameraPositionZ);
        _renderer.Camera.Front = (_settings.CameraFrontX, _settings.CameraFrontY, _settings.CameraFrontZ);

        WorldSettings.SetDaySpeed(_settings.DaySpeed);
        WorldSettings.SetTime(_settings.DayTime);
        WorldSettings.SetRunning(_settings.DayNightCycle);

        _renderer.Camera.SetCameraSpeed(_settings.CameraSpeed);
        
        _renderer.AmbientOcclusion = _settings.AmbientOcclusion;
        _renderer.RenderNormal = _settings.RenderNormal;
        _renderer.RenderWireframe = _settings.RenderWireframe;



        _editorUI.AddElement(_statUI);
        //_editorUI.AddElement(_testUI);
        
        _nodes.Disable();
    }  

    void Exit()
    {
        Console.WriteLine("World node editor exit");

        _settings = new()
        {
            CameraPositionX = _renderer.Camera.Position.X,
            CameraPositionY = _renderer.Camera.Position.Y,
            CameraPositionZ = _renderer.Camera.Position.Z,

            CameraFrontX = _renderer.Camera.Front.X,
            CameraFrontY = _renderer.Camera.Front.Y,
            CameraFrontZ = _renderer.Camera.Front.Z,

            DaySpeed = WorldSettings.DaySpeed,
            DayTime = WorldSettings.ElapsedWorldTime,
            DayNightCycle = !WorldSettings.Paused,

            CameraSpeed = _renderer.Camera.SPEED,

            AmbientOcclusion = _renderer.AmbientOcclusion,
            RenderNormal = _renderer.RenderNormal,
            RenderWireframe = _renderer.RenderWireframe,
        };

        string settingsPath = Game.SettingsPath / "world_editor_settings.json";

        JsonSerializerSettings jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
    
        var json = JsonConvert.SerializeObject(_settings, jsonSettings);
        File.WriteAllText(settingsPath, json);
    }

    private Vector3 _position = Vector3.Zero;
    private Vector3i _chunkPosition = Vector3i.Zero;

    void Update()
    {
        if (GameTime.FpsUpdated)
        {
            _fpsText.UpdateText("FPS: " + GameTime.Fps);
            _managedRamUsageText.UpdateText("Managed Ram: " + GC.GetTotalMemory(false) / (1024 * 1024) + " Mb");
            _ramUsageText.UpdateText("Ram: " + GameTime.Ram / (1024 * 1024) + " Mb");

            _globalChunkSpeed.UpdateText("Chunks/s: " + GlobalCount);
            _generationChunkSpeed.UpdateText("Generation/s: " + GenerationCount);
            _renderingChunkSpeed.UpdateText("Rendering/s: " + RenderingCount);
            _uploadingChunkSpeed.UpdateText("Upload/s: " + UploadCount);

            GlobalCount = 0;
            GenerationCount = 0;
            RenderingCount = 0;
            UploadCount = 0;

            for (int i = 0; i < _generationThreadUsages.Length; i++)
            {
                long ticks = Interlocked.Exchange(ref VoxelRenderer.GenerationThreadTimes[i], 0);
                double seconds = (double)ticks / Stopwatch.Frequency;
                double percent = Math.Min(100.0, seconds * 100.0);
                _generationThreadUsages[i].UpdateText($"Thread {i+1} {percent.Fti()}%");
            }

            for (int i = 0; i < _renderingThreadUsages.Length; i++)
            {
                long ticks = Interlocked.Exchange(ref VoxelRenderer.RenderingThreadTimes[i], 0);
                double seconds = (double)ticks / Stopwatch.Frequency;
                double percent = Math.Min(100.0, seconds * 100.0);
                _renderingThreadUsages[i].UpdateText($"Thread {i+1} {percent.Fti()}%");
            }


            long total = VRAMInfo.GetTotalVRAM();
            long free = VRAMInfo.GetFreeVRAM();
            long used = VRAMInfo.GetUsedVRAM();
            float percentage = VRAMInfo.GetVRAMUsagePercentage();
            VramTotalText.SetText($"Total VRAM: {FormatBytes(total)}").UpdateCharacters();
            VramFreeText.SetText($"Free VRAM: {FormatBytes(free)}").UpdateCharacters();
            VramUsedText.SetText($"Used VRAM: {FormatBytes(used)}").UpdateCharacters();
            VramPrecentText.SetText($"Usage: {percentage:F1}%").UpdateCharacters();
        }

        if (_renderer.Camera.Position != _position)
        {
            _position = _renderer.Camera.Position;
            _positionText.UpdateText($"X Y Z: {_renderer.Camera.Position.X} {_renderer.Camera.Position.Y} {_renderer.Camera.Position.Z}");

            var relative = VoxelData.BlockToChunkRelative(Mathf.FloorToInt(_renderer.Camera.Position));
            if (relative != _chunkPosition)
            {
                _chunkPosition = relative;
                _chunkPositionText.UpdateText($"Chunk X Y Z: {relative.X*32} {relative.Y*32} {relative.Z*32}");

                if (_renderer.GetChunk(relative, out var chunk))
                {
                    _chunkStatusText.UpdateText($"Status: {chunk.Status}");
                    _allocationsCountText.UpdateText($"Allocations: {chunk.Allocations.Count}");
                }
            }
        }

        _vertexCountText.UpdateText("Vertex Count: " + (VoxelRenderer.TotalVertexCount * 6));
        _generationQueueCountText.UpdateText("Generating: " + _renderer.GenQueueCount);
        _renderingQueueCountText.UpdateText("Rendering: " + _renderer.RenderingQueueCount);
        _uploadingQueueCountText.UpdateText("Uploading: " + _renderer.UploadQueue.Count);
        _dictionnaryCountText.UpdateText("Dictionnary: " + _renderer.ChunkDictionnary.Count);
        _cacheCountText.UpdateText("Cache: " + ChunkGeneration.V256FCacheCount);

        if (_sw.Elapsed.TotalSeconds - _time > 0.2f)
        {
            _dataPoolQueueCountText.UpdateText("Data Pools: " + _renderer.DataPool.DataPool.Count);
            _aliveChunksText.UpdateText("Alive Chunks: " + VoxelChunk.AliveChunks.Count);
            _time = _sw.Elapsed.TotalSeconds;

            _averageChunkGenText.UpdateText("Generate: " + ChunkGenerationTimer.GetAverage().ToString("F4") + " ms");
            _averageChunkReadText.UpdateText("Render: " + ChunkRenderingTimer.GetAverage().ToString("F4") + " ms");
            _averageFrameUploadTimeText.UpdateText("Upload: " + TotalUploadTimer.GetAverage().ToString("F4") + " ms");
        }

        _frameTimeGraph.AdvancePoint(GameTime.DeltaTime + 0.1f);

        if (Input.IsKeyPressed(Key.B))
        {
            WriteFinalShader();
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }

    private void WriteFinalShader()
    {
        if (!File.Exists(_baseWorldComputeShaderPath))
            return;

        var lines = File.ReadAllLines(_baseWorldComputeShaderPath);
        var compiledLines = _nodes.GetGLSLCode().Split('\n');

        List<string> newLines = [];

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line == "#code")
            {    
                foreach (var compiledLine in compiledLines)
                {
                    newLines.Add("    " + compiledLine);
                }
            }
            else
            {
                newLines.Add(lines[i]);
            }
        }

        File.WriteAllLines(_finalWorldComputeShaderPath, newLines);
    }

    private UIElementBase _statUI => 
    new UICol(w_full, h_full)[
        new UIVCol(grow_children, top_left, spacing_[10])[
            new UIVCol(grow_children, spacing_[5])[
                new UIText("FPS: ", mc_[10], top_left, bg_white, fs_[1.2f]).Out(out _fpsText),
                new UIText("X Y Z: ", mc_[50], top_left, bg_white, fs_[1.2f]).Out(out _positionText),
                new UIText("Vertex Count: ", mc_[50], top_left, bg_white, fs_[1.2f]).Out(out _vertexCountText)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIText("Chunk X Y Z: ", mc_[50], top_left, bg_white, fs_[1.2f]).Out(out _chunkPositionText),
                new UIText("Status: ", mc_[30], top_left, bg_white, fs_[1.2f]).Out(out _chunkStatusText),
                new UIText("Allocations: ", mc_[30], top_left, bg_white, fs_[1.2f]).Out(out _allocationsCountText)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _globalChunkSpeed),
                new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _generationChunkSpeed),
                new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _renderingChunkSpeed),
                new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _uploadingChunkSpeed)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIText("Generating: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _generationQueueCountText),
                new UIText("Rendering: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _renderingQueueCountText),
                new UIText("Uploading: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _uploadingQueueCountText),
                new UIText("Data Pools: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _dataPoolQueueCountText),
                new UIText("Dictionnary: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _dictionnaryCountText),
                new UIText("Cache: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _cacheCountText)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIText("Generate: ", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _averageChunkGenText),
                new UIText("Render: ", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _averageChunkReadText),
                new UIText("Upload: ", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _averageFrameUploadTimeText)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIText("Alive Chunks: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _aliveChunksText),
                new UIText("Managed Ram: ", mc_[25], top_left, bg_white, fs_[1.2f]).Out(out _managedRamUsageText),
                new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out _ramUsageText),
                new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out VramTotalText),
                new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out VramFreeText),
                new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out VramUsedText),
                new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]).Out(out VramPrecentText)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIHCol(h_[11], spacing_[5])[
                    new UIText("Camera Speed:", fs_[1.2f]),
                    new UICol(grow_children, blank_sharp, gray_[30], alpha_[0.4f], border_[2, 2, 2, 2], middle_left)[
                        new UIField(""+Camera.SPEED, middle_center, mc_[5], text_type_numeric, top_left, bg_white, fs_[1.2f]).OnTextChange(f => _renderer.Camera.SetCameraSpeed(f.GetFloat()))
                    ]
                ],
                new UIHCol(h_[11], spacing_[5])[
                    new UIText("Day Speed:", fs_[1.2f]),
                    new UICol(grow_children, blank_sharp, gray_[30], alpha_[0.4f], border_[2, 2, 2, 2], middle_left)[
                        new UIField(""+WorldSettings.DaySpeed, middle_center, mc_[5], text_type_decimal, top_left, bg_white, fs_[1.2f]).OnTextChange(f => WorldSettings.SetDaySpeed(f.GetFloat()))
                    ]
                ],
                new UIHCol(h_[11], spacing_[5])[
                    new UIText("Day Time:", fs_[1.2f]),
                    new UICol(grow_children, blank_sharp, gray_[30], alpha_[0.4f], border_[2, 2, 2, 2], middle_left)[
                        new UIField(""+WorldSettings.ElapsedWorldTime, middle_center, mc_[5], text_type_decimal, top_left, bg_white, fs_[1.2f]).OnTextChange(f => WorldSettings.SetTime(f.GetFloat()))
                    ]
                ],
                GetToggle("Ambient Occlusion:", _renderer.AmbientOcclusion, b => _renderer.AmbientOcclusion = b),
                GetToggle("Day Night Cycle:", !WorldSettings.Paused, WorldSettings.SetRunning),
                GetToggle("Render normal:", _renderer.RenderNormal, b => _renderer.RenderNormal = b),
                GetToggle("Render wireframe:", _renderer.RenderWireframe, b => _renderer.RenderWireframe = b)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIImg(w_[100], h_[100], bg_white, item_["grass_block"])
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIField("Generation thread usage", top_left, bg_white, fs_[1.2f]),
                new Forloop(0, VoxelRenderer.GenerationThreads, i =>
                {
                    return new UIField($"Thread {i+1} 100%", mc_[13], top_left, bg_white, fs_[1.2f]).Out(out _generationThreadUsages[i]);
                })
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIField("Rendering thread usage", top_left, bg_white, fs_[1.2f]),
                new Forloop(0, VoxelRenderer.RenderingThreads, i =>
                {
                    return new UIField($"Thread {i+1} 100%", mc_[13], top_left, bg_white, fs_[1.2f]).Out(out _renderingThreadUsages[i]);
                })
            ]
        ],
        new UIGraph(bottom_right, w_full, h_[400], graph_points_[200], bg_red).Out(out _frameTimeGraph)
    ];


    private UICol GetToggle(string name, bool baseState, Action<bool> action) => 
    new UIHCol(h_[11], spacing_[5])[
        new UIText(name, fs_[1.2f]),
        new UIButton(w_[11], h_[11], data_["state", baseState], blank_sharp, baseState ? bg_white : new StyleArray(gray_[30], alpha_[0.2f]), border_ui_[2, 2, 2, 2], border_color_g_[100], middle_left).OnClick(btn =>
        {
            bool state = !btn.Dataset.Bool("state");
            btn.Dataset["state"] = state;
            btn.UpdateColor(state ? Vector4.One : new Vector4(0.3f, 0.3f, 0.3f, 0.2f));
            action(state);
        })
    ];

    private UIElementBase _testUI => 
    new UIVCol(grow_children, top_right, spacing_[5])[
        new Foreach<string, Action<VoxelChunk>>(ChunkTestGenerators.Generators, (name, action) =>
        {
            return new UICol(w_[200], h_[50], blank_sharp, bg_white).OnClick(_ => OnNoiseClick(name, action))[
                new UIText(name, middle_center, bg_black, fs_[1.2f])
            ];
        })
    ];

    private void OnNoiseClick(string name, Action<VoxelChunk> action)
    {
        Console.WriteLine("setting action as: " + name);
        VoxelRenderer.ChunkAction = action;
        foreach (var (_, chunk) in _renderer.ChunkDictionnary)
        {
            chunk.ClearBlocks();
            chunk.Status = PBG.NewVoxel.ChunkStatus.Empty;
            _renderer.EnqueueGeneration(chunk);
        }
    }

    struct WorldEditorSettings
    {
        public float CameraPositionX;
        public float CameraPositionY;
        public float CameraPositionZ;

        public float CameraFrontX;
        public float CameraFrontY;
        public float CameraFrontZ;

        public double DaySpeed;
        public double DayTime;
        public bool DayNightCycle;

        public float CameraSpeed;
        
        public bool AmbientOcclusion;
        public bool RenderNormal;
        public bool RenderWireframe;

        public WorldEditorSettings() {}
    }
}