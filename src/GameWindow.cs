using Silk.NET.Maths;
using Silk.NET.Windowing;

public unsafe class GameWindow
{
    public const int WIDTH = 800;
    public const int HEIGHT = 600;

    public IWindow? window;
    public bool frameBufferResized = false;

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

    public void Run() => window!.Run();

    public void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }

}