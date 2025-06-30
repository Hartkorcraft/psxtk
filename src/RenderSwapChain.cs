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
    public void CleanUpSwapChain(Game game)
    {
        game.vk!.DestroyImageView(game.device, game.depthImageView, null);
        game.vk!.DestroyImage(game.device, game.depthImage, null);
        game.vk!.FreeMemory(game.device, game.depthImageMemory, null);

        foreach (var framebuffer in game.swapChainFramebuffers!)
        {
            game.vk!.DestroyFramebuffer(game.device, framebuffer, null);
        }

        fixed (CommandBuffer* commandBuffersPtr = game.commandBuffers)
        {
            game.vk!.FreeCommandBuffers(game.device, game.commandPool, (uint)game.commandBuffers!.Length, commandBuffersPtr);
        }

        game.graphicsPipeline.CleanUp(game);

        foreach (var imageView in game.swapChainImageViews!)
        {
            game.vk!.DestroyImageView(game.device, imageView, null);
        }

        game.khrSwapChain!.DestroySwapchain(game.device, game.swapChain, null);

        for (int i = 0; i < game.swapChainImages!.Length; i++)
        {
            game.vk!.DestroyBuffer(game.device, game.uniformBuffers![i], null);
            game.vk!.FreeMemory(game.device, game.uniformBuffersMemory![i], null);
        }

        game.vk!.DestroyDescriptorPool(game.device, game.descriptorPool, null);
    }

    public void RecreateSwapChain(Game game)
    {
        Vector2D<int> framebufferSize = game.window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = game.window.FramebufferSize;
            game.window.DoEvents();
        }

        game.vk!.DeviceWaitIdle(game.device);

        game.renderSwapChain.CleanUpSwapChain(game);

        game.CreateSwapChain();
        game.CreateImageViews();
        game.graphicsPipeline.CreateRenderPass(game);
        game.graphicsPipeline.CreateGraphicsPipeline(game);
        game.CreateDepthResources();
        game.CreateFramebuffers();
        game.CreateUniformBuffers();
        game.CreateDescriptorPool();
        game.CreateDescriptorSets();
        game.CreateCommandBuffers();

        game.imagesInFlight = new Fence[game.swapChainImages!.Length];
    }
}