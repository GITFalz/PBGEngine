using System.Reflection;
using System.Runtime.Intrinsics.X86;
using PBG.Core;
using PBG.Data;
using PBG.Graphics.Vulkan;
using PBG.Threads;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace PBG.Graphics;

public unsafe class VulkanInstance
{
    public static VulkanInstance Instance = null!;
    private IWindow _window;
    #if DEBUG
        const bool enableValidationLayers = true;
    #else
        const bool enableValidationLayers = false;
    #endif

    public GameWindow gameWindow;

    public VulkanDevice VulkanDevice;

    public VulkanSwapchain VulkanSwapchain;
    public VulkanImage VulkanImage;
    public VulkanBuffer VulkanBuffer;

    private VulkanImageViews _vulkanImageViews;

    public VulkanRenderPass LoadRenderPass;
    public VulkanRenderPass ClearRenderPass;
    public VulkanRenderPass FramebufferLoadRenderPass;
    public VulkanRenderPass FramebufferClearRenderPass;

    private VulkanCommandBuffers _vulkanCommandBuffers;
    public VulkanDepthBuffer VulkanDepthBuffer;
    private VulkanFramebuffer _vulkanFramebuffer;
    private VulkanSyncObject _vulkanSyncObject;


    private VulkanQuery _vulkanQuery;

    public uint CurrentFrame = 0;
    public Framebuffer CurrentFramebuffer;

    public bool _isLoading = true;
    
    public VulkanInstance(GameWindow gameWindow, int width, int height)
    {
        Instance = this;

        var options = WindowOptions.DefaultVulkan;

        options.Size   = new Vector2D<int>(width, height);
        options.Title  = "My First Silk.NET Window";
        options.VSync  = false;

        Window.PrioritizeGlfw();
        _window = Window.Create(options);

        this.gameWindow = gameWindow;
    }

    public void Run()
    {
        _window.Load   += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnResize;

        _window.Run();

        _window.Dispose();
    } 

    private void InitVulkan()
    {
        VulkanDevice = new VulkanDevice(_window, enableValidationLayers);
        VulkanImage = new VulkanImage(VulkanDevice);
        
        VulkanBuffer = new VulkanBuffer(VulkanDevice);
        
        VulkanSwapchain = new VulkanSwapchain(VulkanDevice, _window);
        VulkanDepthBuffer = new VulkanDepthBuffer(VulkanDevice, VulkanSwapchain, VulkanImage);   

        _vulkanImageViews = new VulkanImageViews(VulkanDevice, VulkanImage, VulkanSwapchain);       

        ClearRenderPass = new VulkanRenderPass(VulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.Undefined, ImageLayout.PresentSrcKhr, AttachmentLoadOp.Clear);
        LoadRenderPass = new VulkanRenderPass(VulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr, AttachmentLoadOp.Load);
        FramebufferClearRenderPass = new VulkanRenderPass(VulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.ColorAttachmentOptimal, ImageLayout.ShaderReadOnlyOptimal, AttachmentLoadOp.Clear);
        FramebufferLoadRenderPass = new VulkanRenderPass(VulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.ColorAttachmentOptimal, ImageLayout.ShaderReadOnlyOptimal, AttachmentLoadOp.Load);

        _vulkanCommandBuffers = new VulkanCommandBuffers(VulkanDevice);
        
        _vulkanFramebuffer = new VulkanFramebuffer(VulkanDevice, VulkanSwapchain, _vulkanImageViews, VulkanDepthBuffer, LoadRenderPass);
        _vulkanSyncObject = new VulkanSyncObject(VulkanDevice, VulkanSwapchain);

        _vulkanQuery = new VulkanQuery(VulkanDevice);

        _ = new GFX(this, VulkanDevice, _window, VulkanSwapchain, VulkanImage, VulkanBuffer, _vulkanImageViews, _vulkanCommandBuffers, VulkanDepthBuffer, _vulkanFramebuffer, _vulkanSyncObject, _vulkanQuery);  
    }

    private void OnLoad()
    {
        Console.WriteLine($"Window loaded - {Game.Width}x{Game.Height}");

        // === SSE family ===
        Console.WriteLine($"SSE      : {Sse.IsSupported}");
        Console.WriteLine($"SSE2     : {Sse2.IsSupported}");
        Console.WriteLine($"SSE3     : {Sse3.IsSupported}");
        Console.WriteLine($"SSSE3    : {Ssse3.IsSupported}");
        Console.WriteLine($"SSE4.1   : {Sse41.IsSupported}");
        Console.WriteLine($"SSE4.2   : {Sse42.IsSupported}");

        // === AVX family ===
        Console.WriteLine($"AVX      : {Avx.IsSupported}");
        Console.WriteLine($"AVX2     : {Avx2.IsSupported}");
        Console.WriteLine($"FMA      : {Fma.IsSupported}");
        Console.WriteLine($"AVXVNNI  : {AvxVnni.IsSupported}");

        // === Bit manipulation ===
        Console.WriteLine($"BMI1     : {Bmi1.IsSupported}");
        Console.WriteLine($"BMI2     : {Bmi2.IsSupported}");
        Console.WriteLine($"LZCNT    : {Lzcnt.IsSupported}");
        Console.WriteLine($"POPCNT   : {Popcnt.IsSupported}");

        // === Crypto / special ===
        Console.WriteLine($"AES      : {Aes.IsSupported}");
        Console.WriteLine($"PCLMULQDQ: {Pclmulqdq.IsSupported}");

        // === AVX-512 ===
        Console.WriteLine($"AVX-512F : {Avx512F.IsSupported}");
        Console.WriteLine($"AVX-512BW: {Avx512BW.IsSupported}");
        Console.WriteLine($"AVX-512CD: {Avx512CD.IsSupported}");
        Console.WriteLine($"AVX-512DQ: {Avx512DQ.IsSupported}");
        Console.WriteLine($"AVX-512VL: {Avx512F.VL.IsSupported}");   // note: VL lives under Avx512F
        Console.WriteLine($"AVX-512VBMI : {Avx512Vbmi.IsSupported}");

        // === Newer / less common ===
        Console.WriteLine($"AVX10v1  : {Avx10v1.IsSupported}");
        Console.WriteLine($"X86Base  : {X86Base.IsSupported}");
        Console.WriteLine($"SERIALIZE: {X86Serialize.IsSupported}");

        var input = _window.CreateInput();

        // Keyboard
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += gameWindow.OnKeyDown;
            keyboard.KeyUp += gameWindow.OnKeyUp;
            keyboard.KeyChar += gameWindow.OnKeyChar;
        }

        // Mouse
        foreach (var mouse in input.Mice)
        {
            mouse.MouseMove += (mouse, position) => gameWindow.OnMouseMove(mouse, position);
            mouse.MouseDown += gameWindow.OnMouseDown;
            mouse.MouseUp += gameWindow.OnMouseUp;
            mouse.Scroll += gameWindow.OnScroll;
        }

        gameWindow.Keyboard = input.Keyboards[0];
        gameWindow.Mouse = input.Mice[0];
         
        InitVulkan();

        VRAMInfo.Initialize(this);

        var initAttributes = AttributeManager.GetOrderedAttribute<InternalSystemInitAttribute, int>(a => (int)a.Attribute.Priority);
        AttributeManager.InvokeAttributeMethod(initAttributes, "Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        gameWindow.OnLoad();
    }

    private void OnResize(Vector2D<int> vector2D)
    {
        Game.Width = vector2D.X;
        Game.Height = vector2D.Y;

        if (Game.Width == 0 || Game.Height == 0) 
            return;
            
        RecreateSwapChain();
        
        gameWindow.OnResize(Game.Width, Game.Height);
        BufferBase.ResizeAll((uint)Game.Width, (uint)Game.Height);
    }

    private void OnUpdate(double deltaSeconds)
    {
        if (_isLoading)
            return;

        MainThreadDispatcher.ProcessQueue();
            
        BufferBase.DisposeCached();
        gameWindow.OnUpdate(deltaSeconds);
    }

    public void RecreateSwapChain() 
    {
        while (_window.FramebufferSize.X == 0 || _window.FramebufferSize.Y == 0)
        {
            _window.DoEvents();
        }

        Console.WriteLine("Allocated: " + _vulkanSyncObject.AllocatedValue + " - Completed: " + _vulkanSyncObject.CompletedValue);
        VulkanDevice.Vk.DeviceWaitIdle(VulkanDevice.Device);

        VulkanDepthBuffer.Dispose();
        _vulkanFramebuffer.Dispose();
        _vulkanImageViews.Dispose();
        VulkanSwapchain.Dispose();

        VulkanSwapchain = new VulkanSwapchain(VulkanDevice, _window);
        VulkanDepthBuffer = new VulkanDepthBuffer(VulkanDevice, VulkanSwapchain, VulkanImage);
        _vulkanImageViews = new VulkanImageViews(VulkanDevice, VulkanImage, VulkanSwapchain);
        _vulkanFramebuffer = new VulkanFramebuffer(VulkanDevice, VulkanSwapchain, _vulkanImageViews, VulkanDepthBuffer, LoadRenderPass);

        _ = new GFX(this, VulkanDevice, _window, VulkanSwapchain, VulkanImage, VulkanBuffer, _vulkanImageViews, _vulkanCommandBuffers, VulkanDepthBuffer, _vulkanFramebuffer, _vulkanSyncObject, _vulkanQuery);  
    }

    private void OnRender(double deltaSeconds)
    {
        VulkanDevice.Vk.WaitForFences(VulkanDevice.Device, 1, ref _vulkanSyncObject.InFlightFences[CurrentFrame], true, ulong.MaxValue);

        uint imageIndex;
        Result result = VulkanDevice.KhrSwapchain.AcquireNextImage(VulkanDevice.Device, VulkanSwapchain.SwapChain, ulong.MaxValue, _vulkanSyncObject.ImageAvailableSemaphores[CurrentFrame], default, &imageIndex);
        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException("failed to acquire swap chain image!");

        VulkanDevice.Vk.ResetFences(VulkanDevice.Device, 1, ref _vulkanSyncObject.InFlightFences[CurrentFrame]);

        VulkanDevice.Vk.ResetCommandBuffer(_vulkanCommandBuffers.CommandBuffers[CurrentFrame], 0);

        RecordCommandBuffer(_vulkanCommandBuffers.CommandBuffers[CurrentFrame], imageIndex);

        ulong computeSignalValue = _vulkanSyncObject.AllocateTimelineValue();

        SemaphoreSubmitInfo[] graphicsWaits =
        [
            new()
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = _vulkanSyncObject.ImageAvailableSemaphores[CurrentFrame],
                StageMask = PipelineStageFlags2.ColorAttachmentOutputBit
            },/*
            new()
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = _vulkanSyncObject.TimelineSemaphore,
                Value = computeSignalValue,
                StageMask = PipelineStageFlags2.VertexShaderBit
            },*/
        ];
        SemaphoreSubmitInfo graphicsSignal = new()
        {
            SType = StructureType.SemaphoreSubmitInfo,
            Semaphore = _vulkanSyncObject.RenderFinishedSemaphores[CurrentFrame],
            StageMask = PipelineStageFlags2.AllCommandsBit
        };

        CommandBufferSubmitInfo graphicsCmdInfo = new()
        {
            SType = StructureType.CommandBufferSubmitInfo,
            CommandBuffer = _vulkanCommandBuffers.CommandBuffers[CurrentFrame]
        };

        fixed (SemaphoreSubmitInfo* pWaits = graphicsWaits)
        {
            SubmitInfo2 graphicsSubmit = new()
            {
                SType = StructureType.SubmitInfo2,
                WaitSemaphoreInfoCount = (uint)graphicsWaits.Length,
                PWaitSemaphoreInfos = pWaits,
                CommandBufferInfoCount = 1,
                PCommandBufferInfos = &graphicsCmdInfo,
                SignalSemaphoreInfoCount = 1,
                PSignalSemaphoreInfos = &graphicsSignal
            };

            if (VulkanDevice.Vk.QueueSubmit2(VulkanDevice.GraphicsQueue, 1, &graphicsSubmit, _vulkanSyncObject.InFlightFences[CurrentFrame]) != Result.Success)
                throw new InvalidOperationException("failed to submit draw command buffer!");
        }

        /*
        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var waitSemaphore = _vulkanSyncObject.ImageAvailableSemaphores[CurrentFrame];
        var computeCommandBuffer = _vulkanCommandBuffers.ComputeCommandBuffers[CurrentFrame];
        var commandBuffer = _vulkanCommandBuffers.CommandBuffers[CurrentFrame];
        var signalSemaphore = _vulkanSyncObject.RenderFinishedSemaphores[CurrentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };

        if (VulkanDevice.Vk.QueueSubmit(VulkanDevice.GraphicsQueue, 1, &submitInfo, _vulkanSyncObject.InFlightFences[CurrentFrame]) != Result.Success)
            throw new InvalidOperationException("failed to submit draw command buffer!");
            */

        var signalSemaphore = _vulkanSyncObject.RenderFinishedSemaphores[CurrentFrame];
        var swapChains = stackalloc SwapchainKHR[] { VulkanSwapchain.SwapChain };

        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex,
            PResults = null
        };
        

        result = VulkanDevice.KhrSwapchain.QueuePresent(VulkanDevice.PresentQueue, &presentInfo);
        if (result == Result.ErrorOutOfDateKhr)
            RecreateSwapChain();
        else if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException("failed to present swap chain image!");
            

        CurrentFrame = (CurrentFrame + 1) % GFX.MAX_FRAMES_IN_FLIGHT;
    }

    private void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex) 
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        if (VulkanDevice.Vk.BeginCommandBuffer(commandBuffer, &beginInfo) != Result.Success) {
            throw new InvalidOperationException("failed to begin recording command buffer!");
        }

        CurrentFramebuffer = _vulkanFramebuffer.SwapChainFramebuffers[imageIndex];

        gameWindow.OnCompute();

        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = ClearRenderPass.RenderPass,
            Framebuffer = _vulkanFramebuffer.SwapChainFramebuffers[imageIndex]
        };
        renderPassInfo.RenderArea.Offset = new(0, 0);
        renderPassInfo.RenderArea.Extent = VulkanSwapchain.SwapChainExtent;

        ClearValue[] clearValues = new ClearValue[2];
        clearValues[0].Color = new(0.04f, 0.2f, 0.7f, 1f);
        clearValues[1].DepthStencil = new(1.0f, 0);

        renderPassInfo.ClearValueCount = (uint)clearValues.Length;
        fixed (ClearValue* pClearValues = clearValues)
        renderPassInfo.PClearValues = pClearValues;

        VulkanDevice.Vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);

        Rect2D scissor = new()
        {
            Offset = new(0, 0),
            Extent = VulkanSwapchain.SwapChainExtent
        };
        VulkanDevice.Vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

        GFX.Viewport(0, 0, VulkanSwapchain.SwapChainExtent.Width, VulkanSwapchain.SwapChainExtent.Height);

        FBO.currentRenderPassState = FBO.RenderPassState.Main;

        if (_isLoading)
        {
            gameWindow.OnRenderLoad();
            _isLoading = false;
        }
        else
        {
            gameWindow.OnRender();
        }

        FBO.ResetAll();

        VulkanDevice.Vk.CmdEndRenderPass(commandBuffer);

        if (VulkanDevice.Vk.EndCommandBuffer(commandBuffer) != Result.Success) {
            throw new InvalidOperationException("failed to record command buffer!");
        }
    }

    public void OnClosing()
    {
        var cleanupAttributes = AttributeManager.GetAttribute<InternalSystemCleanupAttribute>();
        AttributeManager.InvokeAttributeMethod(cleanupAttributes, "Cleanup", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        VulkanDevice.Vk.DeviceWaitIdle(VulkanDevice.Device);

        BufferBase.DisposeAll();

        VulkanDepthBuffer.Dispose();
        _vulkanFramebuffer.Dispose();
        _vulkanImageViews.Dispose();
        VulkanSwapchain.Dispose();

        LoadRenderPass.Dispose();
        ClearRenderPass.Dispose();
        FramebufferLoadRenderPass.Dispose();
        FramebufferClearRenderPass.Dispose();
        
        _vulkanSyncObject.Dispose();
        _vulkanCommandBuffers.Dispose();

        _vulkanQuery.Dispose();

        VulkanDevice.Dispose();

        gameWindow.OnUnload();
    }

    public void Dispose()
    {
        
    }
}