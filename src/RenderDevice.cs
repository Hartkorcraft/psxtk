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

public unsafe class RenderDevice
{
    public PhysicalDevice physicalDevice;
    public Device device;

    public void PickPhysicalDevice(Game game)
    {
        var devices = game.vk!.GetPhysicalDevices(game.instance);

        foreach (var device in devices)
        {
            if (game.IsDeviceSuitable(device))
            {
                physicalDevice = device;
                break;
            }
        }

        if (physicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    public void CreateLogicalDevice(Game game)
    {
        var indices = game.FindQueueFamilies(physicalDevice);

        var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        PhysicalDeviceFeatures deviceFeatures = new()
        {
            SamplerAnisotropy = true,
        };


        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = (uint)game.deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(game.deviceExtensions)
        };

        if (game.enableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)game.validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(game.validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (game.vk!.CreateDevice(physicalDevice, in createInfo, null, out device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        game.vk!.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out game.graphicsQueue);
        game.vk!.GetDeviceQueue(device, indices.PresentFamily!.Value, 0, out game.presentQueue);

        if (game.enableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

    }
}