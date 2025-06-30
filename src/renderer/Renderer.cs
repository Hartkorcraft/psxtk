using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class Renderer
{
    public RenderSwapChain renderSwapChain = new();
    public Commands commands = new();

    public void InitRenderLoop(Game game)
    {
        game.gameWindow.window!.Render += (delta) => DrawFrame(game, delta);
    }

    void DrawFrame(Game game, double delta)
    {
        game.vk!.WaitForFences(game.renderDevice.device, 1, in renderSwapChain.inFlightFences![renderSwapChain.currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        var result = renderSwapChain.khrSwapChain!.AcquireNextImage(game.renderDevice.device, renderSwapChain.swapChain, ulong.MaxValue, renderSwapChain.imageAvailableSemaphores![renderSwapChain.currentFrame], default, ref imageIndex);

        if (result == Result.ErrorOutOfDateKhr)
        {
            renderSwapChain.RecreateSwapChain(game);
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("failed to acquire swap chain image!");
        }

        game.renderBuffer.UpdateUniformBuffer(game, imageIndex);

        if (renderSwapChain.imagesInFlight![imageIndex].Handle != default)
        {
            game.vk!.WaitForFences(game.renderDevice.device, 1, in renderSwapChain.imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        renderSwapChain.imagesInFlight[imageIndex] = renderSwapChain.inFlightFences[renderSwapChain.currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        var waitSemaphores = stackalloc[] { renderSwapChain.imageAvailableSemaphores[renderSwapChain.currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var commandsBuffer = commands.commandBuffers![imageIndex];

        submitInfo = submitInfo with
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,

            CommandBufferCount = 1,
            PCommandBuffers = &commandsBuffer
        };

        var signalSemaphores = stackalloc[] { renderSwapChain.renderFinishedSemaphores![renderSwapChain.currentFrame] };
        submitInfo = submitInfo with
        {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        game.vk!.ResetFences(game.renderDevice.device, 1, in renderSwapChain.inFlightFences[renderSwapChain.currentFrame]);

        if (game.vk!.QueueSubmit(game.renderDevice.graphicsQueue, 1, in submitInfo, renderSwapChain.inFlightFences[renderSwapChain.currentFrame]) != Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }

        var swapChains = stackalloc[] { renderSwapChain.swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        result = renderSwapChain.khrSwapChain.QueuePresent(game.renderDevice.presentQueue, in presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || game.gameWindow.frameBufferResized)
        {
            game.gameWindow.frameBufferResized = false;
            renderSwapChain.RecreateSwapChain(game);
        }
        else if (result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }

        renderSwapChain.currentFrame = (renderSwapChain.currentFrame + 1) % RenderSwapChain.MAX_FRAMES_IN_FLIGHT;
    }
}