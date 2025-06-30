using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

public unsafe class GraphicsSurface
{
    public KhrSurface? khrSurface;
    public SurfaceKHR surface;

    public void Clean(Game game)
    {
        khrSurface!.DestroySurface(game.graphicsInstance.instance, surface, null);
    }

    public void Init(Game game)
    {
        if (!game.vk!.TryGetInstanceExtension<KhrSurface>(game.graphicsInstance.instance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = game.gameWindow.window!.VkSurface!.Create<AllocationCallbacks>(game.graphicsInstance.instance.ToHandle(), null).ToSurface();
    }
}