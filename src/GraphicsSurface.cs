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

    public void CreateSurface(Game game)
    {
        if (!game.vk!.TryGetInstanceExtension<KhrSurface>(game.instance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = game.window!.VkSurface!.Create<AllocationCallbacks>(game.instance.ToHandle(), null).ToSurface();
    }

}