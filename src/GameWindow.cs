using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public unsafe class GameWindow
{
    public const int WIDTH = 800;
    public const int HEIGHT = 600;

    public IWindow? window;
    public bool frameBufferResized = false;
    IInputContext? inputContext;

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
        inputContext = window.CreateInput();
        inputContext.Keyboards[0].KeyDown += OnKeyDown;
        inputContext.Keyboards[0].KeyUp += OnKeyUp;
        inputContext.Keyboards[0].KeyChar += OnKeyChar;
    }

    public void Run() => window!.Run();

    public void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }


    void OnKeyChar(IKeyboard keyboard, char c)
    {
    }

    void OnKeyUp(IKeyboard keyboard, Key key, int i)// chuj wie czym jest i
    {
    }

    void OnKeyDown(IKeyboard keyboard, Key key, int i)// chuj wie czym jest i
    {
    }
}