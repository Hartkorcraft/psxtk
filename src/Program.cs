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

    // public const string TEXTURE_PATH = @"Assets\viking_room.png";


    // public bool enableValidationLayers = true;

    // public readonly string[] validationLayers =
    // [
    //     "VK_LAYER_KHRONOS_validation"
    // ];

    public readonly string[] deviceExtensions =
    [
        KhrSwapchain.ExtensionName
    ];

    public GameWindow gameWindow = new();
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



    public Model model = new();

    public void Run()
    {
        gameWindow.InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
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

        graphicsPipeline.Init(this);

        renderDevice.CreateCommandPool(this);


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

        renderSwapChain.CreateSyncObjects(this);
    }

    public void MainLoop()
    {
        graphicsPipeline.InitRenderLoop(this);
        gameWindow.Run();
        vk!.DeviceWaitIdle(renderDevice.device);
    }

    void DestroyBuffers()
    {
        vk!.DestroyBuffer(renderDevice.device, indexBuffer, null);
        vk!.FreeMemory(renderDevice.device, renderBuffer.indexBufferMemory, null);

        vk!.DestroyBuffer(renderDevice.device, vertexBuffer, null);
        vk!.FreeMemory(renderDevice.device, renderBuffer.vertexBufferMemory, null);
    }

    public void CleanUp()
    {
        imageSampler.Destroy(this);
        renderImage.Destroy(this);

        descriptors.Destroy(this);
        DestroyBuffers();

        renderSwapChain.Destroy(this);

        renderDevice.Destroy(this);

        debugTools.Destroy(this);

        graphicsSurface.Clean(this);
        vk!.DestroyInstance(graphicsInstance.instance, null);
        vk!.Dispose();

        gameWindow.window?.Dispose();
    }
}