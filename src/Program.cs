using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Assimp;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

var app = new Game();
app.Run();

public unsafe class Game
{
    public const int WIDTH = 800;
    public const int HEIGHT = 600;

    // public const string TEXTURE_PATH = @"Assets\viking_room.png";

    public const int MAX_FRAMES_IN_FLIGHT = 2;

    // public bool enableValidationLayers = true;

    // public readonly string[] validationLayers =
    // [
    //     "VK_LAYER_KHRONOS_validation"
    // ];

    public readonly string[] deviceExtensions =
    [
        KhrSwapchain.ExtensionName
    ];

    public IWindow? window;
    public Vk? vk;

    public GraphicsInstance graphicsInstance = new();

    public DebugTools debugTools = new DebugTools();

    public GraphicsSurface graphicsSurface = new GraphicsSurface();

    public RenderDevice renderDevice = new();

    public Queue graphicsQueue;
    public Queue presentQueue;

    public GraphicsPipeline graphicsPipeline = new();
    public RenderSwapChain renderSwapChain = new();

    public Image depthImage;
    public DeviceMemory depthImageMemory;
    public ImageView depthImageView;

    public RenderImage renderImage = new();
    public ImageSampler imageSampler = new();

    public Buffer vertexBuffer;
    public Buffer indexBuffer;


    public RenderBuffer renderBuffer = new();

    public Buffer[]? uniformBuffers;
    public DeviceMemory[]? uniformBuffersMemory;

    public Descriptors descriptors = new();

    public Commands commands = new();

    public Semaphore[]? imageAvailableSemaphores;
    public Semaphore[]? renderFinishedSemaphores;
    public Fence[]? inFlightFences;
    public Fence[]? imagesInFlight;
    public int currentFrame = 0;

    public bool frameBufferResized = false;

    public Vertex[]? vertices;

    public uint[]? indices;

    public Model model = new();

    public void Run()
    {
        InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    public void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(WIDTH, HEIGHT),
            Title = "Vulkan",
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        window.Resize += FramebufferResizeCallback;
    }

    public void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }

    public void InitVulkan()
    {
        graphicsInstance.CreateInstance(this);
        debugTools.SetupDebugMessenger(this);
        graphicsSurface.CreateSurface(this);
        renderDevice.PickPhysicalDevice(this);
        renderDevice.CreateLogicalDevice(this);
        renderSwapChain.CreateSwapChain(this);
        renderSwapChain.CreateImageViews(this);
        graphicsPipeline.CreateRenderPass(this);
        graphicsPipeline.CreateDescriptorSetLayout(this);
        graphicsPipeline.CreateGraphicsPipeline(this);
        renderDevice.CreateCommandPool(this);
        graphicsPipeline.CreateDepthResources(this);
        renderSwapChain.CreateFramebuffers(this);
        renderImage.CreateTextureImage(this);
        renderImage.CreateTextureImageView(this);
        imageSampler.CreateTextureSampler(this);
        model.LoadModel(this);
        renderBuffer.CreateVertexBuffer(this);
        renderBuffer.CreateIndexBuffer(this);
        renderBuffer.CreateUniformBuffers(this);
        descriptors.CreateDescriptorPool(this);
        descriptors.CreateDescriptorSets(this);
        commands.CreateCommandBuffers(this);
        CreateSyncObjects();
    }

    public void MainLoop()
    {
        window!.Render += (delta) => graphicsPipeline.DrawFrame(this, delta);
        window!.Run();
        vk!.DeviceWaitIdle(renderDevice.device);
    }

    public void CleanUp()
    {
        renderSwapChain.CleanUpSwapChain(this);

        vk!.DestroySampler(renderDevice.device, imageSampler.textureSampler, null);
        vk!.DestroyImageView(renderDevice.device, renderImage.textureImageView, null);

        vk!.DestroyImage(renderDevice.device, renderImage.textureImage, null);
        vk!.FreeMemory(renderDevice.device, renderImage.textureImageMemory, null);

        vk!.DestroyDescriptorSetLayout(renderDevice.device, graphicsPipeline.descriptorSetLayout, null);

        vk!.DestroyBuffer(renderDevice.device, indexBuffer, null);
        vk!.FreeMemory(renderDevice.device, renderBuffer.indexBufferMemory, null);

        vk!.DestroyBuffer(renderDevice.device, vertexBuffer, null);
        vk!.FreeMemory(renderDevice.device, renderBuffer.vertexBufferMemory, null);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk!.DestroySemaphore(renderDevice.device, renderFinishedSemaphores![i], null);
            vk!.DestroySemaphore(renderDevice.device, imageAvailableSemaphores![i], null);
            vk!.DestroyFence(renderDevice.device, inFlightFences![i], null);
        }

        vk!.DestroyCommandPool(renderDevice.device, renderDevice.commandPool, null);

        vk!.DestroyDevice(renderDevice.device, null);

        if (debugTools.enableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugTools.debugUtils!.DestroyDebugUtilsMessenger(graphicsInstance.instance, debugTools.debugMessenger, null);
        }

        graphicsSurface.khrSurface!.DestroySurface(graphicsInstance.instance, graphicsSurface.surface, null);
        vk!.DestroyInstance(graphicsInstance.instance, null);
        vk!.Dispose();

        window?.Dispose();
    }

    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        vk!.GetPhysicalDeviceMemoryProperties(renderDevice.physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new Exception("failed to find suitable memory type!");
    }

    public void CreateSyncObjects()
    {
        imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        imagesInFlight = new Fence[renderSwapChain.swapChainImages!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (vk!.CreateSemaphore(renderDevice.device, in semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                vk!.CreateSemaphore(renderDevice.device, in semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                vk!.CreateFence(renderDevice.device, in fenceInfo, null, out inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }


}