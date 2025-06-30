using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;

var app = new Game();
app.Run();

public unsafe class Game
{
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

    public GraphicsPipeline graphicsPipeline = new();

    public Renderer renderer = new();

    public RenderImage renderImage = new();
    public ImageSampler imageSampler = new();

    public Buffer vertexBuffer;
    public Buffer indexBuffer;

    public RenderBuffer renderBuffer = new();

    public Descriptors descriptors = new();


    public Model model = new();

    public Camera camera = new();

    public void Run()
    {
        gameWindow.InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    public void InitVulkan()
    {
        graphicsInstance.Init(this);

        debugTools.Init(this);

        graphicsSurface.Init(this);

        renderDevice.Init(this);

        renderer.renderSwapChain.CreateSwapChain(this);
        renderer.renderSwapChain.CreateImageViews(this);

        graphicsPipeline.Init(this);

        renderer.renderSwapChain.CreateFramebuffers(this);

        renderImage.CreateTextureImage(this);
        renderImage.CreateTextureImageView(this);

        imageSampler.Init(this);

        model.Init(this);

        renderBuffer.CreateVertexBuffer(this);
        renderBuffer.CreateIndexBuffer(this);
        renderBuffer.CreateUniformBuffers(this);

        descriptors.Init(this);

        renderer.commands.CreateCommandBuffers(this);

        renderer.renderSwapChain.CreateSyncObjects(this);
    }

    public void MainLoop()
    {
        renderer.InitRenderLoop(this);
        gameWindow.window!.Update += (delta) => Update(this, delta);

        gameWindow.Run();
        vk!.DeviceWaitIdle(renderDevice.device);
    }

    void Update(Game game, double delta)
    {
        var moveSpeed = 20f * (float)delta;

        if (Input.Instance.IsKeyPressed(Key.W))
        {
            //Move forwards
            camera.CameraPosition += moveSpeed * camera.CameraFront;
        }
        if (Input.Instance.IsKeyPressed(Key.S))
        {
            //Move backwards
            camera.CameraPosition -= moveSpeed * camera.CameraFront;
        }
        if (Input.Instance.IsKeyPressed(Key.A))
        {
            //Move left
            var v = Vector3.Normalize(Vector3.Cross(camera.CameraFront, camera.CameraUp)) * moveSpeed;
            camera.CameraPosition -= Vector3.Normalize(Vector3.Cross(camera.CameraFront, camera.CameraUp)) * moveSpeed;
        }
        if (Input.Instance.IsKeyPressed(Key.D))
        {
            //Move right
            camera.CameraPosition += Vector3.Normalize(Vector3.Cross(camera.CameraFront, camera.CameraUp)) * moveSpeed;
        }
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

        renderer.renderSwapChain.Destroy(this);

        renderDevice.Destroy(this);

        debugTools.Destroy(this);

        graphicsSurface.Clean(this);
        vk!.DestroyInstance(graphicsInstance.instance, null);
        vk!.Dispose();

        gameWindow.window?.Dispose();
    }
}
