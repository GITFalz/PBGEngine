[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SystemInitAttribute : Attribute {
    public InitPriority Priority { get; }
    public SystemInitAttribute(InitPriority priority = InitPriority.Global) {
        Priority = priority;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal class InternalSystemInitAttribute : Attribute {
    public InitPriority Priority { get; }
    public InternalSystemInitAttribute(InitPriority priority = InitPriority.Global) {
        Priority = priority;
    }
}

public enum InitPriority
{
    // Core engine systems - initialize first
    EngineCore      = -100,   // Very early: memory allocators, logging, platform layer, etc.
    NativeInterop   = -90,    // Native bindings, GPU device, etc.
    GraphicsDevice  = -80,    // Create graphics device / context

    // Resource management
    Buffer          = -50,    // Buffers (your original)
    Texture         = -40,
    Shader          = -30,
    Material        = -20,
    Mesh            = -10,

    // Data & assets
    Data            = 0,      // Your original (kept for compatibility)
    AssetLoader     = 10,
    ResourceManager = 20,

    // Game systems
    Input           = 50,
    Physics         = 60,
    Animation       = 70,
    Audio           = 80,
    AI              = 90,

    // Global / script-related (important for reloadable DLLs)
    Global          = 100,    // Your original
    ScriptSystem    = 110,    // Script manager, hot-reload system
    EntitySystem    = 120,    // ECS / entity-component system
    SceneManager    = 130,

    // Late initialization
    Rendering       = 200,    // Final rendering pipeline setup
    UI              = 210,
    Debug           = 220,    // Debug tools, overlays, etc.

    // Default / fallback
    Default         = 500
}