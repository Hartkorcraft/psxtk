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

public unsafe class Descriptors
{
    public DescriptorPool descriptorPool;
    public DescriptorSet[]? descriptorSets;

    public void CreateDescriptorPool(Game game)
    {
        var poolSizes = new DescriptorPoolSize[]
        {
            new DescriptorPoolSize()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)game.renderSwapChain.swapChainImages!.Length,
            },
            new DescriptorPoolSize()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)game.renderSwapChain.swapChainImages!.Length,
            }
        };

        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
        {

            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = (uint)game.renderSwapChain.swapChainImages!.Length,
            };

            if (game.vk!.CreateDescriptorPool(game.renderDevice.device, in poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }

        }
    }

    public void CreateDescriptorSets(Game game)
    {
        var layouts = new DescriptorSetLayout[game.renderSwapChain.swapChainImages!.Length];
        Array.Fill(layouts, game.graphicsPipeline.descriptorSetLayout);

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = (uint)game.renderSwapChain.swapChainImages!.Length,
                PSetLayouts = layoutsPtr,
            };

            descriptorSets = new DescriptorSet[game.renderSwapChain.swapChainImages.Length];
            fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
            {
                if (game.vk!.AllocateDescriptorSets(game.renderDevice.device, in allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }

        for (int i = 0; i < game.renderSwapChain.swapChainImages.Length; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = game.uniformBuffers![i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

            };

            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = game.renderImage.textureImageView,
                Sampler = game.imageSampler.textureSampler,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet =descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                game.vk!.UpdateDescriptorSets(game.renderDevice.device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
            }
        }
    }
}