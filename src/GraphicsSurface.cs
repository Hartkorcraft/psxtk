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

public unsafe class GraphicsSurface
{
    public KhrSurface? khrSurface;
    public SurfaceKHR surface;

    public void Clean(Game game)
    {
        khrSurface!.DestroySurface(game.graphicsInstance.instance, surface, null);
    }

    public void CreateSurface(Game game)
    {
        if (!game.vk!.TryGetInstanceExtension<KhrSurface>(game.graphicsInstance.instance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = game.gameWindow.window!.VkSurface!.Create<AllocationCallbacks>(game.graphicsInstance.instance.ToHandle(), null).ToSurface();
    }
}