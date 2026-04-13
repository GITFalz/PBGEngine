using System.Reflection;
using PBG.Core;
using PBG.Data;
using PBG.Files;
using PBG.Graphics.Vulkan;
using PBG.Mathematics;
using PBG.UI;
using PBG.Voxel;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace PBG.Graphics;

public unsafe class Renderer : IDisposable
{
    private IWindow _window;
    private bool enableValidation;

    private VulkanDevice _vulkanDevice;

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
    
    public VoxelEngine Engine;
    //public Scene Scene;

    public Renderer(IWindow window, bool enableValidation)
    {
        Engine = VoxelEngine.Instance;
        _window = window;
        this.enableValidation = enableValidation;
    }

    public void Run()
    {
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnResize;

        _window.Run();
    }

    private void OnLoad()
    {
        try
        {
            var input = _window.CreateInput();

            // Keyboard
            foreach (var keyboard in input.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
                keyboard.KeyChar += OnKeyChar;
            }

            // Mouse
            foreach (var mouse in input.Mice)
            {
                mouse.MouseMove += (mouse, position) => OnMouseMove(mouse, position);
                mouse.MouseDown += OnMouseDown;
                mouse.MouseUp += OnMouseUp;
                mouse.Scroll += OnScroll;
            }

            Engine.Keyboard = input.Keyboards[0];
            Engine.SetMouse(input.Mice[0]);
        
            InitVulkan();

            string engineDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engine.dll");

            if (!File.Exists(engineDllPath))
            {
                engineDllPath = (PString)AppDomain.CurrentDomain.BaseDirectory / ".." / ".." / ".." / ".." / "Engine" / "bin" / "Debug" / "net9.0" / "Engine.dll";
            }

            var engineAssembly = Assembly.LoadFrom(engineDllPath);

            var initAttributes = AttributeManager.GetOrderedAttribute<SystemInitAttribute, int>(a => (int)a.Attribute.Priority);
            AttributeManager.InvokeAttributeMethod(initAttributes, "Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Input.Start(input.Mice[0]);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        Data.Input.OnKeyDown((Data.Key)key);
        UIController.InputField((Data.Key)key);
    }

    public void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int scanCode)
    {
        Data.Input.OnKeyUp((Data.Key)key);
    }

    public void OnKeyChar(IKeyboard keyboard, char c)
    {
        
    }
    
    public void OnMouseMove(IMouse mouse, Vector2 position)
    {
        
    }
    
    public void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        Data.Input.OnMouseDown((Data.MouseButton)button);
    }
    
    public void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        Data.Input.OnMouseUp((Data.MouseButton)button);
    }
    
    public void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        Data.Input.OnMouseWheel((scroll.X, scroll.Y));
    }
    

    private void InitVulkan()
    {
        _vulkanDevice = new VulkanDevice(_window, enableValidation);
        VulkanImage = new VulkanImage(_vulkanDevice);
        
        VulkanBuffer = new VulkanBuffer(_vulkanDevice);
        
        VulkanSwapchain = new VulkanSwapchain(_vulkanDevice, _window);
        VulkanDepthBuffer = new VulkanDepthBuffer(_vulkanDevice, VulkanSwapchain, VulkanImage);   

        _vulkanImageViews = new VulkanImageViews(_vulkanDevice, VulkanImage, VulkanSwapchain);       

        ClearRenderPass = new VulkanRenderPass(_vulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.Undefined, ImageLayout.PresentSrcKhr, AttachmentLoadOp.Clear);
        LoadRenderPass = new VulkanRenderPass(_vulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr, AttachmentLoadOp.Load);
        FramebufferClearRenderPass = new VulkanRenderPass(_vulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.ColorAttachmentOptimal, ImageLayout.ShaderReadOnlyOptimal, AttachmentLoadOp.Clear);
        FramebufferLoadRenderPass = new VulkanRenderPass(_vulkanDevice, VulkanSwapchain.SwapChainImageFormat, VulkanDepthBuffer.DepthImageFormat, ImageLayout.ColorAttachmentOptimal, ImageLayout.ShaderReadOnlyOptimal, AttachmentLoadOp.Load);

        _vulkanCommandBuffers = new VulkanCommandBuffers(_vulkanDevice);
        
        _vulkanFramebuffer = new VulkanFramebuffer(_vulkanDevice, VulkanSwapchain, _vulkanImageViews, VulkanDepthBuffer, LoadRenderPass);
        _vulkanSyncObject = new VulkanSyncObject(_vulkanDevice, VulkanSwapchain);

        _ = new GFX(this, _vulkanDevice, _window, VulkanSwapchain, VulkanImage, VulkanBuffer, _vulkanImageViews, _vulkanCommandBuffers, VulkanDepthBuffer, _vulkanFramebuffer, _vulkanSyncObject);  
    }

    private void OnResize(Vector2D<int> vector2D)
    {
        VoxelEngine.Width = vector2D.X;
        VoxelEngine.Height = vector2D.Y;

        if (VoxelEngine.Width == 0 || VoxelEngine.Height == 0) 
            return;

        RecreateSwapChain();

        SceneLoop.ResizeInternal();
    }

    private void RecreateSwapChain() 
    {
        while (_window.FramebufferSize.X == 0 || _window.FramebufferSize.Y == 0)
        {
            _window.DoEvents();
        }

        _vulkanDevice.Vk.DeviceWaitIdle(_vulkanDevice.Device);

        VulkanDepthBuffer.Dispose();
        _vulkanFramebuffer.Dispose();
        _vulkanImageViews.Dispose();
        VulkanSwapchain.Dispose();

        VulkanSwapchain = new VulkanSwapchain(_vulkanDevice, _window);
        VulkanDepthBuffer = new VulkanDepthBuffer(_vulkanDevice, VulkanSwapchain, VulkanImage);
        _vulkanImageViews = new VulkanImageViews(_vulkanDevice, VulkanImage, VulkanSwapchain);
        _vulkanFramebuffer = new VulkanFramebuffer(_vulkanDevice, VulkanSwapchain, _vulkanImageViews, VulkanDepthBuffer, LoadRenderPass);
    }

    private void OnUpdate(double deltaSeconds)
    {
        Input.Update();
        GameTime.Update((float)deltaSeconds);
        BufferBase.DisposeCached();
        
        SceneLoop.UpdateInternal();    

        Input.LateUpdate();
    }

    private double _timer = 0;
    private int _counter = 0;

    private void OnRender(double deltaSeconds)
    {
        _vulkanDevice.Vk.WaitForFences(_vulkanDevice.Device, 1, ref _vulkanSyncObject.InFlightFences[CurrentFrame], true, ulong.MaxValue);

        uint imageIndex;
        Result result = _vulkanDevice.KhrSwapchain.AcquireNextImage(_vulkanDevice.Device, VulkanSwapchain.SwapChain, ulong.MaxValue, _vulkanSyncObject.ImageAvailableSemaphores[CurrentFrame], default, &imageIndex);
        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException("failed to acquire swap chain image!");

        _vulkanDevice.Vk.ResetFences(_vulkanDevice.Device, 1, ref _vulkanSyncObject.InFlightFences[CurrentFrame]);

        _vulkanDevice.Vk.ResetCommandBuffer(_vulkanCommandBuffers.CommandBuffers[CurrentFrame], 0);
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

        if (_vulkanDevice.Vk.QueueSubmit(_vulkanDevice.GraphicsQueue, 1, &submitInfo, _vulkanSyncObject.InFlightFences[CurrentFrame]) != Result.Success)
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

        result = _vulkanDevice.KhrSwapchain.QueuePresent(_vulkanDevice.PresentQueue, &presentInfo);
        if (result == Result.ErrorOutOfDateKhr)
            RecreateSwapChain();
        else if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException("failed to present swap chain image!");

        CurrentFrame = (CurrentFrame + 1) % GFX.MAX_FRAMES_IN_FLIGHT;
        if (_timer >= 1f)
        {
            Console.WriteLine(_counter);
            _timer = 0;
            _counter = 0;     
        }
        _timer += deltaSeconds;
        _counter++;
    }

    private void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex) 
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = 0, // Optional
            PInheritanceInfo = null // Optional
        };

        if (_vulkanDevice.Vk.BeginCommandBuffer(commandBuffer, &beginInfo) != Result.Success) {
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
        clearValues[0].Color = new(0.53f * 0.5f, 0.81f * 0.5f, 0.92f * 0.5f, 1.0f);
        clearValues[1].DepthStencil = new(1.0f, 0);

        renderPassInfo.ClearValueCount = (uint)clearValues.Length;
        fixed (ClearValue* pClearValues = clearValues)
        renderPassInfo.PClearValues = pClearValues;

        _vulkanDevice.Vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);

        Rect2D scissor = new()
        {
            Offset = new(0, 0),
            Extent = VulkanSwapchain.SwapChainExtent
        };
        _vulkanDevice.Vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

        GFX.Viewport(0, 0, VulkanSwapchain.SwapChainExtent.Width, VulkanSwapchain.SwapChainExtent.Height);
        
        SceneLoop.RenderInternal();
        /*
        if (_isLoading)
        {
            gameWindow.OnRenderLoad();
            _isLoading = false;
        }
        else
        {
            MeshRenderer.Count = 0;
            MeshRenderer.Time = 0;

            gameWindow.OnRender();
            //UIController.ClearFrameBuffer();
            EditorScene.Render();
            UIController.GlobalRender();
        }
        
        if (_timer >= 1f)
        {
            Console.WriteLine(stopwatch.Elapsed.TotalMicroseconds + " µs " + MeshRenderer.Time + " µs " + MeshRenderer.Count); 
        }
        */

        _vulkanDevice.Vk.CmdEndRenderPass(commandBuffer);

        if (_vulkanDevice.Vk.EndCommandBuffer(commandBuffer) != Result.Success) {
            throw new InvalidOperationException("failed to record command buffer!");
        }
    }

    public void OnClosing()
    {
        _vulkanDevice.Vk.DeviceWaitIdle(_vulkanDevice.Device);

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

        _vulkanDevice.Dispose();
    }

    public void Dispose()
    {
        
    }
}