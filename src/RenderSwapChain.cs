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

public unsafe class RenderSwapChain
{
    public const int MAX_FRAMES_IN_FLIGHT = 2;

    public KhrSwapchain? khrSwapChain;
    public SwapchainKHR swapChain;
    public Image[]? swapChainImages;
    public Format swapChainImageFormat;
    public Extent2D swapChainExtent;
    public ImageView[]? swapChainImageViews;
    public Framebuffer[]? swapChainFramebuffers;

    public Semaphore[]? imageAvailableSemaphores;
    public Semaphore[]? renderFinishedSemaphores;
    public Fence[]? inFlightFences;
    public Fence[]? imagesInFlight;
    public int currentFrame = 0;

    public void Destroy(Game game)
    {
        CleanUpSwapChain(game);
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            game.vk!.DestroySemaphore(game.renderDevice.device, renderFinishedSemaphores![i], null);
            game.vk!.DestroySemaphore(game.renderDevice.device, imageAvailableSemaphores![i], null);
            game.vk!.DestroyFence(game.renderDevice.device, inFlightFences![i], null);
        }
    }

    public void CleanUpSwapChain(Game game)
    {
        game.vk!.DestroyImageView(game.renderDevice.device, game.depthImageView, null);
        game.vk!.DestroyImage(game.renderDevice.device, game.depthImage, null);
        game.vk!.FreeMemory(game.renderDevice.device, game.depthImageMemory, null);

        foreach (var framebuffer in swapChainFramebuffers!)
        {
            game.vk!.DestroyFramebuffer(game.renderDevice.device, framebuffer, null);
        }

        fixed (CommandBuffer* commandBuffersPtr = game.commands.commandBuffers)
        {
            game.vk!.FreeCommandBuffers(game.renderDevice.device, game.renderDevice.commandPool, (uint)game.commands.commandBuffers!.Length, commandBuffersPtr);
        }

        game.graphicsPipeline.CleanUp(game);

        foreach (var imageView in swapChainImageViews!)
        {
            game.vk!.DestroyImageView(game.renderDevice.device, imageView, null);
        }

        khrSwapChain!.DestroySwapchain(game.renderDevice.device, swapChain, null);

        for (int i = 0; i < swapChainImages!.Length; i++)
        {
            game.vk!.DestroyBuffer(game.renderDevice.device, game.uniformBuffers![i], null);
            game.vk!.FreeMemory(game.renderDevice.device, game.uniformBuffersMemory![i], null);
        }

        game.vk!.DestroyDescriptorPool(game.renderDevice.device, game.descriptors.descriptorPool, null);
    }

    public void CreateSwapChain(Game game)
    {
        var swapChainSupport = QuerySwapChainSupport(game, game.renderDevice.physicalDevice);

        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(game, swapChainSupport.Capabilities);

        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = game.graphicsSurface.surface,

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var indices = game.renderDevice.FindQueueFamilies(game, game.renderDevice.physicalDevice);
        var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

        if (indices.GraphicsFamily != indices.PresentFamily)
        {
            creatInfo = creatInfo with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            creatInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        creatInfo = creatInfo with
        {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
        };

        if (khrSwapChain is null)
        {
            if (!game.vk!.TryGetDeviceExtension(game.graphicsInstance.instance, game.renderDevice.device, out khrSwapChain))
            {
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");
            }
        }

        if (khrSwapChain!.CreateSwapchain(game.renderDevice.device, in creatInfo, null, out swapChain) != Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        khrSwapChain.GetSwapchainImages(game.renderDevice.device, swapChain, ref imageCount, null);
        swapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = swapChainImages)
        {
            khrSwapChain.GetSwapchainImages(game.renderDevice.device, swapChain, ref imageCount, swapChainImagesPtr);
        }

        swapChainImageFormat = surfaceFormat.Format;
        swapChainExtent = extent;
    }

    public void RecreateSwapChain(Game game)
    {
        Vector2D<int> framebufferSize = game.gameWindow.window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = game.gameWindow.window.FramebufferSize;
            game.gameWindow.window.DoEvents();
        }

        game.vk!.DeviceWaitIdle(game.renderDevice.device);

        game.renderSwapChain.CleanUpSwapChain(game);

        CreateSwapChain(game);
        CreateImageViews(game);
        game.graphicsPipeline.CreateRenderPass(game);
        game.graphicsPipeline.CreateGraphicsPipeline(game);
        game.graphicsPipeline.CreateDepthResources(game);
        CreateFramebuffers(game);
        game.renderBuffer.CreateUniformBuffers(game);
        game.descriptors.CreateDescriptorPool(game);
        game.descriptors.CreateDescriptorSets(game);
        game.commands.CreateCommandBuffers(game);

        imagesInFlight = new Fence[swapChainImages!.Length];
    }

    public Extent2D ChooseSwapExtent(Game game, SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            var framebufferSize = game.gameWindow.window!.FramebufferSize;

            Extent2D actualExtent = new()
            {
                Width = (uint)framebufferSize.X,
                Height = (uint)framebufferSize.Y
            };

            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    public SwapChainSupportDetails QuerySwapChainSupport(Game game, PhysicalDevice physicalDevice)
    {
        var details = new SwapChainSupportDetails();

        game.graphicsSurface.khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, game.graphicsSurface.surface, out details.Capabilities);

        uint formatCount = 0;
        game.graphicsSurface.khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, game.graphicsSurface.surface, ref formatCount, null);

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
            {
                game.graphicsSurface.khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, game.graphicsSurface.surface, ref formatCount, formatsPtr);
            }
        }
        else
        {
            details.Formats = Array.Empty<SurfaceFormatKHR>();
        }

        uint presentModeCount = 0;
        game.graphicsSurface.khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, game.graphicsSurface.surface, ref presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* formatsPtr = details.PresentModes)
            {
                game.graphicsSurface.khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, game.graphicsSurface.surface, ref presentModeCount, formatsPtr);
            }

        }
        else
        {
            details.PresentModes = Array.Empty<PresentModeKHR>();
        }

        return details;
    }

    SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    public void CreateImageViews(Game game)
    {
        game.renderSwapChain.swapChainImageViews = new ImageView[game.renderSwapChain.swapChainImages!.Length];

        for (int i = 0; i < game.renderSwapChain.swapChainImages.Length; i++)
        {
            game.renderSwapChain.swapChainImageViews[i] = game.renderImage.CreateImageView(game, game.renderSwapChain.swapChainImages[i], game.renderSwapChain.swapChainImageFormat, ImageAspectFlags.ColorBit, 1);
        }
    }

    public void CreateFramebuffers(Game game)
    {
        game.renderSwapChain.swapChainFramebuffers = new Framebuffer[game.renderSwapChain.swapChainImageViews!.Length];

        for (int i = 0; i < game.renderSwapChain.swapChainImageViews.Length; i++)
        {
            var attachments = new[] { game.renderSwapChain.swapChainImageViews[i], game.depthImageView };

            fixed (ImageView* attachmentsPtr = attachments)
            {
                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = game.graphicsPipeline.renderPass,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    Width = game.renderSwapChain.swapChainExtent.Width,
                    Height = game.renderSwapChain.swapChainExtent.Height,
                    Layers = 1,
                };

                if (game.vk!.CreateFramebuffer(game.renderDevice.device, in framebufferInfo, null, out game.renderSwapChain.swapChainFramebuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                }
            }
        }
    }

    public void CreateSyncObjects(Game game)
    {
        imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        imagesInFlight = new Fence[game.renderSwapChain.swapChainImages!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (game.vk!.CreateSemaphore(game.renderDevice.device, in semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                game.vk!.CreateSemaphore(game.renderDevice.device, in semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                game.vk!.CreateFence(game.renderDevice.device, in fenceInfo, null, out inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }
}