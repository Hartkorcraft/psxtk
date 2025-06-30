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

public unsafe class DebugTools
{
    public bool enableValidationLayers = true;

    public readonly string[] validationLayers =
    [
        "VK_LAYER_KHRONOS_validation"
    ];

    public ExtDebugUtils? debugUtils;
    public DebugUtilsMessengerEXT debugMessenger;

    public void SetupDebugMessenger(Game game)
    {
        if (!enableValidationLayers) return;

        //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!game.vk!.TryGetInstanceExtension(game.graphicsInstance.instance, out debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        game.PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(game.graphicsInstance.instance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }
}