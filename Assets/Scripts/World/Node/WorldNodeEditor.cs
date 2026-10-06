using System.Diagnostics;
using PBG;
using PBG.Core;
using PBG.Data;
using PBG.MathLibrary;
using PBG.NewVoxel;
using PBG.Nodes;
using PBG.UI;
using Newtonsoft.Json;
using PBG.Graphics;

public class WorldNodeEditor : ScriptingNode
{
    private PBGNodes _nodes = null!;

    private UIController _editorUI = null!;
    
    private VoxelRenderer _renderer = null!;

    private List<Action> _frameUpdates = [];
    private List<Action> _200msUpdates = [];
    private List<Action> _1000msUpdates = [];
    private List<Action> _positionUpdates = [];
    private List<Action> _chunkUpdates = [];
    private List<Action<VoxelChunk>> _chunkSuccessUpdates = [];
    private List<Action> _chunkFailUpdates = [];

    private BoundingBoxRenderer _boxRenderer;

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
    
    public VoxelChunk? _currentChunk = null;

    void Start()
    {
        _nodes = Transform.Scene.QueryComponent<PBGNodes>();
        _editorUI = Transform.GetComponent<UIController>();
        _renderer = Transform.Scene.QueryComponent<VoxelRenderer>();
        _boxRenderer = Transform.Scene.QueryComponent<BoundingBoxRenderer>();


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

    private static void RunUpdates(List<Action> updates)
    {
        for (int i = 0; i < updates.Count; i++)
            updates[i].Invoke();
    }

    private static void RunUpdates<T>(List<Action<T>> updates, T value)
    {
        for (int i = 0; i < updates.Count; i++)
            updates[i].Invoke(value);
    }

    void Update()
    {
        if (GameTime.FpsUpdated)
        {
            RunUpdates(_1000msUpdates);
        }

        if (_renderer.Camera.Position != _position)
        {
            _position = _renderer.Camera.Position;
            RunUpdates(_positionUpdates);

            var relative = VoxelData.BlockToChunkRelative(Mathf.FloorToInt(_position));
            if (relative != _chunkPosition)
            {
                _chunkPosition = relative;
                RunUpdates(_chunkUpdates);

                if (_renderer.GetChunk(0, relative, out var chunk))
                {
                    _currentChunk = chunk;
                    RunUpdates(_chunkSuccessUpdates, chunk);
                }
                else
                {
                    _currentChunk = null;
                    RunUpdates(_chunkFailUpdates);
                }
            }
        }

        RunUpdates(_frameUpdates);

        if (_sw.Elapsed.TotalSeconds - _time > 0.2f)
        {
            RunUpdates(_200msUpdates);

            _time = _sw.Elapsed.TotalSeconds;
        }

        if (Input.IsKeyPressed(Key.Number1) && _currentChunk != null)
        {
            List<BoundingBoxData> boundingBoxDatas = [];
            foreach (var allocation in _currentChunk.Allocations)
            {
                var datapool = allocation.DataPool;
                var chunkInfo = datapool.ChunkInfo[allocation.Offset];
                boundingBoxDatas.Add(new()
                {
                    Position = chunkInfo.Center - 16,
                    Size = (32, 32, 32),
                    Color = (1, 0, 0, 0.7f)
                });
            }
            _boxRenderer.UpdateBoundingBoxes([..boundingBoxDatas]);
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

    private static UIText TextUpdate(UIText text, List<Action> updates, Func<string> updateText)
    {
        updates.Add(() => text.UpdateText(updateText()));
        return text;
    }

    private static UIText TextUpdate<T>(UIText text, List<Action<T>> updates, Func<T, string> updateText)
    {
        updates.Add(value => text.UpdateText(updateText(value)));
        return text;
    }

    private UIText UpdateFrame(UIText text, Func<string> updateText) => TextUpdate(text, _frameUpdates, updateText);
    private UIText Update200Ms(UIText text, Func<string> updateText) => TextUpdate(text, _200msUpdates, updateText);
    private UIText Update1000Ms(UIText text, Func<string> updateText) => TextUpdate(text, _1000msUpdates, updateText);
    private UIText UpdatePosition(UIText text, Func<string> updateText) => TextUpdate(text, _positionUpdates, updateText);
    private UIText UpdateChunk(UIText text, Func<string> updateText) => TextUpdate(text, _chunkUpdates, updateText);
    private UIText UpdateChunk(UIText text, Func<VoxelChunk, string> successText, Func<string> failText)
    {
        TextUpdate(text, _chunkSuccessUpdates, successText);
        TextUpdate(text, _chunkFailUpdates, failText);
        return text;
    }
    private UIText UpdateChunkSuccess(UIText text, Func<VoxelChunk, string> successText) => TextUpdate(text, _chunkSuccessUpdates, successText);
    private UIText UpdateChunkFail(UIText text, Func<string> failText) => TextUpdate(text, _chunkFailUpdates, failText);

    private UIElementBase _statUI => 
    new UICol(w_full, h_full)[
        new UIVCol(grow_children, top_left, spacing_[10])[
            new UIVCol(grow_children, spacing_[5])[
                Update1000Ms(new UIText("FPS: ", mc_[10], top_left, bg_white, fs_[1.2f]), () => "FPS: " + GameTime.Fps),
                UpdatePosition(new UIText("X Y Z: ", mc_[50], top_left, bg_white, fs_[1.2f]), () => $"X Y Z: {_position.X} {_position.Y} {_position.Z}"),
                UpdateFrame(new UIText("Vertex Count: ", mc_[50], top_left, bg_white, fs_[1.2f]), () => "Vertex Count: " + (VoxelRenderer.TotalVertexCount * 6))
            ],
            new UIVCol(grow_children, spacing_[5])[
                UpdateChunk(new UIText("Chunk X Y Z: ", mc_[50], top_left, bg_white, fs_[1.2f]), () => $"Chunk X Y Z: {_chunkPosition.X*32} {_chunkPosition.Y*32} {_chunkPosition.Z*32}"),
                UpdateChunk(new UIText("Status: ", mc_[30], top_left, bg_white, fs_[1.2f]), chunk => $"Status: {chunk.Status}", () => $"Status: None"),
                UpdateChunk(new UIText("Allocations: ", mc_[30], top_left, bg_white, fs_[1.2f]), chunk => $"Allocations: {chunk.Allocations.Count}", () => $"Allocations: None")
            ],
            new UIVCol(grow_children, spacing_[5])[
                Update1000Ms(new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Chunks/s: " + GlobalCount.Take()),
                Update1000Ms(new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Generation/s: " + GenerationCount.Take()),
                Update1000Ms(new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Rendering/s: " + RenderingCount.Take()),
                Update1000Ms(new UIText("", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Upload/s: " + UploadCount.Take())
            ],
            new UIVCol(grow_children, spacing_[5])[
                UpdateFrame(new UIText("Generating: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Generating: " + _renderer.GenQueueCount),
                UpdateFrame(new UIText("Rendering: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Rendering: " + _renderer.RenderingQueueCount),
                UpdateFrame(new UIText("Uploading: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Uploading: " + _renderer.UploadQueue.Count),
                Update200Ms(new UIText("Data Pools: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Data Pools: " + _renderer.DataPool.DataPool.Count),
                Update200Ms(new UIText("Allocations: ", mc_[25], top_left, bg_white, fs_[1.2f]), () =>
                {
                    int allocations = 0;
                    for (int i = 0; i < _renderer.DataPool.DataPool.Count; i++)
                        allocations += _renderer.DataPool.DataPool[i].AllocationCount;
                    return "Allocations: " + allocations;
                }),
                UpdateFrame(new UIText("Dictionnary: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Dictionnary: " + _renderer.ChunkDictionnary.Count),
                UpdateFrame(new UIText("Cache: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Cache: " + ChunkGeneration.V256FCacheCount)
            ],
            new UIVCol(grow_children, spacing_[5])[
                Update200Ms(new UIText("Generate: ", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Generate: " + ChunkGenerationTimer.GetAverage().ToString("F4") + " ms"),
                Update200Ms(new UIText("Render: ", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Render: " + ChunkRenderingTimer.GetAverage().ToString("F4") + " ms"),
                Update200Ms(new UIText("Upload: ", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Upload: " + TotalUploadTimer.GetAverage().ToString("F4") + " ms")
            ],
            new UIVCol(grow_children, spacing_[5])[
                Update200Ms(new UIText("Alive Chunks: ", mc_[20], top_left, bg_white, fs_[1.2f]), () => "Alive Chunks: " + VoxelChunk.AliveChunks.Count),
                Update1000Ms(new UIText("Managed Ram: ", mc_[25], top_left, bg_white, fs_[1.2f]),   () => "Managed Ram: " + GC.GetTotalMemory(false) / (1024 * 1024) + " Mb"),
                Update1000Ms(new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]),           () => "Ram: " + GameTime.Ram / (1024 * 1024) + " Mb"),
                Update1000Ms(new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]),           () => $"Total VRAM: {FormatBytes(VRAMInfo.GetTotalVRAM())}"),
                Update1000Ms(new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]),           () => $"Free VRAM: {FormatBytes(VRAMInfo.GetFreeVRAM())}"),
                Update1000Ms(new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]),           () => $"Used VRAM: {FormatBytes(VRAMInfo.GetUsedVRAM())}"),
                Update1000Ms(new UIText("Ram: ", mc_[20], top_left, bg_white, fs_[1.2f]),           () => $"Usage: {VRAMInfo.GetVRAMUsagePercentage():F1}%")
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
                Update1000Ms(new UIText("0", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Generation Count: " + _renderer._generationSignal.CurrentCount),
                Update1000Ms(new UIText("0", mc_[25], top_left, bg_white, fs_[1.2f]), () => "Rendering Count: " + _renderer._renderingSignal.CurrentCount)
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIField("Generation thread usage", top_left, bg_white, fs_[1.2f]),
                new Forloop(0, VoxelRenderer.GenerationThreads, i =>
                {
                    return Update1000Ms(new UIField($"Thread {i+1} 100%", mc_[25], top_left, bg_white, fs_[1.2f]), () =>
                    {
                        long ticks = Interlocked.Exchange(ref VoxelRenderer.GenerationThreadTimes[i], 0);
                        double seconds = (double)ticks / Stopwatch.Frequency;
                        double percent = Math.Min(100.0, seconds * 100.0);
                        return $"Thread {i+1} {percent.Fti()}%";
                    });
                })
            ],
            new UIVCol(grow_children, spacing_[5])[
                new UIField("Rendering thread usage", top_left, bg_white, fs_[1.2f]),
                new Forloop(0, VoxelRenderer.RenderingThreads, i =>
                {
                    return Update1000Ms(new UIField($"Thread {i+1} 100%", mc_[25], top_left, bg_white, fs_[1.2f]), () =>
                    {
                        if (VoxelRenderer.RenderingThreadTimes[i] == -1)
                            return $"Thread {i+1} 0% crashed";

                        long ticks = Interlocked.Exchange(ref VoxelRenderer.RenderingThreadTimes[i], 0);
                        double seconds = (double)ticks / Stopwatch.Frequency;
                        double percent = Math.Min(100.0, seconds * 100.0);
                        return $"Thread {i+1} {percent.Fti()}%";
                    });
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