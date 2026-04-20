using System.Reflection;
using PBG.Core;
using PBG.Data;
using PBG.Graphics.Vulkan;
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

        _ = new GFX(this, VulkanDevice, _window, VulkanSwapchain, VulkanImage, VulkanBuffer, _vulkanImageViews, _vulkanCommandBuffers, VulkanDepthBuffer, _vulkanFramebuffer, _vulkanSyncObject);  
    }

    private void OnLoad()
    {
        Console.WriteLine($"Window loaded - {Game.Width}x{Game.Height}");

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
            
        BufferBase.DisposeCached();
        gameWindow.OnUpdate(deltaSeconds);
    }

    public void RecreateSwapChain() 
    {
        while (_window.FramebufferSize.X == 0 || _window.FramebufferSize.Y == 0)
        {
            _window.DoEvents();
        }

        VulkanDevice.Vk.DeviceWaitIdle(VulkanDevice.Device);

        VulkanDepthBuffer.Dispose();
        _vulkanFramebuffer.Dispose();
        _vulkanImageViews.Dispose();
        VulkanSwapchain.Dispose();

        VulkanSwapchain = new VulkanSwapchain(VulkanDevice, _window);
        VulkanDepthBuffer = new VulkanDepthBuffer(VulkanDevice, VulkanSwapchain, VulkanImage);
        _vulkanImageViews = new VulkanImageViews(VulkanDevice, VulkanImage, VulkanSwapchain);
        _vulkanFramebuffer = new VulkanFramebuffer(VulkanDevice, VulkanSwapchain, _vulkanImageViews, VulkanDepthBuffer, LoadRenderPass);
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

        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var waitSemaphore = _vulkanSyncObject.ImageAvailableSemaphores[CurrentFrame];
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
            Flags = 0, // Optional
            PInheritanceInfo = null // Optional
        };

        if (VulkanDevice.Vk.BeginCommandBuffer(commandBuffer, &beginInfo) != Result.Success) {
            throw new InvalidOperationException("failed to begin recording command buffer!");
        }

        CurrentFramebuffer = _vulkanFramebuffer.SwapChainFramebuffers[imageIndex];

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

        VulkanDevice.Dispose();

        gameWindow.OnUnload();

        _window.Dispose();
    }

    public void Dispose()
    {
        
    }
}