using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class Commands
{
    public CommandBuffer[]? commandBuffers;
    public bool IsFrameStarted { get; private set; }

    public void CreateCommandBuffers(Game game)
    {
        commandBuffers = new CommandBuffer[game.renderer.renderSwapChain.swapChainFramebuffers!.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = game.renderDevice.commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (game.vk!.AllocateCommandBuffers(game.renderDevice.device, in allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }


        for (int i = 0; i < commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
            };

            if (game.vk!.BeginCommandBuffer(commandBuffers[i], in beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = game.graphicsPipeline.renderPass,
                Framebuffer = game.renderer.renderSwapChain.swapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = game.renderer.renderSwapChain.swapChainExtent,
                }
            };

            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
                },
                new()
                {
                    DepthStencil = new () { Depth = 1, Stencil = 0 }
                }
            };


            fixed (ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                game.vk!.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            }

            game.vk!.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, game.graphicsPipeline.graphicsPipeline);

            var vertexBuffers = new Buffer[] { game.vertexBuffer };
            var offsets = new ulong[] { 0 };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                game.vk!.CmdBindVertexBuffers(commandBuffers[i], 0, 1, vertexBuffersPtr, offsetsPtr);
            }

            game.vk!.CmdBindIndexBuffer(commandBuffers[i], game.indexBuffer, 0, IndexType.Uint32);

            game.vk!.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, game.graphicsPipeline.pipelineLayout, 0, 1, in game.descriptors.descriptorSets![i], 0, null);

            game.vk!.CmdDrawIndexed(commandBuffers[i], (uint)game.model.indices!.Length, 1, 0, 0, 0);

            game.vk!.CmdEndRenderPass(commandBuffers[i]);

            if (game.vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }
    }

    public CommandBuffer BeginSingleTimeCommands(Game game)
    {
        IsFrameStarted = true;
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = game.renderDevice.commandPool,
            CommandBufferCount = 1,
        };

        game.vk!.AllocateCommandBuffers(game.renderDevice.device, in allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        game.vk!.BeginCommandBuffer(commandBuffer, in beginInfo);

        return commandBuffer;
    }

    public void EndSingleTimeCommands(Game game, CommandBuffer commandBuffer)
    {
        game.vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        game.vk!.QueueSubmit(game.renderDevice.graphicsQueue, 1, in submitInfo, default);
        game.vk!.QueueWaitIdle(game.renderDevice.graphicsQueue);

        game.vk!.FreeCommandBuffers(game.renderDevice.device, game.renderDevice.commandPool, 1, in commandBuffer);

        IsFrameStarted = false;
    }
}