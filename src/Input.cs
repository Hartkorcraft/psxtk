using System.Numerics;
using Silk.NET.Input;

public class Input
{
    public static Input? Instance;
    public static event Action<Key>? OnKeyUp;
    public static event Action<Key>? OnKeyDown;
    public static event Action<IMouse, Vector2>? OnMouseMove;


    IInputContext inputContext;

    public Input(IInputContext inputContext)
    {
        Instance = this;
        this.inputContext = inputContext;

        inputContext.Keyboards[0].KeyDown += KeyDown;
        inputContext.Keyboards[0].KeyUp += KeyUp;
        inputContext.Keyboards[0].KeyChar += KeyChar;
        inputContext.Mice[0].MouseMove += MouseMove;
        inputContext.Mice[0].Click += MouseClick;
        inputContext.Mice[0].MouseUp += MouseUp;
        inputContext.Mice[0].MouseDown += MouseDown;

        inputContext.Mice[0].Cursor.CursorMode = CursorMode.Raw;
        // inputContext.Mice[0].Scroll += OnMouseWheel;
    }


    public bool IsKeyPressed(Key key)
    {
        return inputContext.Keyboards[0].IsKeyPressed(key);
    }

    void MouseDown(IMouse mouse, MouseButton button)
    {
    }

    void MouseUp(IMouse mouse, MouseButton button)
    {
    }

    void MouseClick(IMouse mouse, MouseButton button, Vector2 vector)
    {
    }

    void MouseMove(IMouse mouse, Vector2 pos)
    {
        OnMouseMove?.Invoke(mouse, pos);
    }

    void KeyChar(IKeyboard keyboard, char c)
    {
    }

    void KeyUp(IKeyboard keyboard, Key key, int i)// chuj wie czym jest i
    {
        OnKeyUp?.Invoke(key);
    }

    void KeyDown(IKeyboard keyboard, Key key, int i)// chuj wie czym jest i
    {
        OnKeyDown?.Invoke(key);
    }
}