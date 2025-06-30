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
    public CommandPool commandPool;

    public void PickPhysicalDevice(Game game)
    {
        var devices = game.vk!.GetPhysicalDevices(game.graphicsInstance.instance);

        foreach (var device in devices)
        {
            if (IsDeviceSuitable(game, device))
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
        var indices = FindQueueFamilies(game, physicalDevice);

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

        if (game.debugTools.enableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)game.debugTools.validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(game.debugTools.validationLayers);
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

        if (game.debugTools.enableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }

    public bool IsDeviceSuitable(Game game, PhysicalDevice device)
    {
        var indices = FindQueueFamilies(game, device);

        bool extensionsSupported = CheckDeviceExtensionsSupport(game, device);

        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            var swapChainSupport = game.renderSwapChain.QuerySwapChainSupport(game, device);
            swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
        }

        game.vk!.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);

        return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
    }

    public bool CheckDeviceExtensionsSupport(Game game, PhysicalDevice device)
    {
        uint extentionsCount = 0;
        game.vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

        var availableExtensions = new ExtensionProperties[extentionsCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            game.vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return game.deviceExtensions.All(availableExtensionNames.Contains);
    }

    public QueueFamilyIndices FindQueueFamilies(Game game, PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        game.vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            game.vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }

        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            game.graphicsSurface.khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, game.graphicsSurface.surface, out var presentSupport);

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    public void CreateCommandPool(Game game)
    {
        var queueFamiliyIndicies = FindQueueFamilies(game, game.renderDevice.physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
        };

        if (game.vk!.CreateCommandPool(game.renderDevice.device, in poolInfo, null, out commandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }
}