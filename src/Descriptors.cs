using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class Descriptors
{
    public DescriptorPool descriptorPool;
    public DescriptorSet[]? descriptorSets;

    public void Init(Game game)
    {
        CreateDescriptorPool(game);
        CreateDescriptorSets(game);
    }

    public void Destroy(Game game)
    {
        game.vk!.DestroyDescriptorSetLayout(game.renderDevice.device, game.graphicsPipeline.descriptorSetLayout, null);
    }

    public void CreateDescriptorPool(Game game)
    {
        var poolSizes = new DescriptorPoolSize[]
        {
            new DescriptorPoolSize()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)game.renderer.renderSwapChain.swapChainImages!.Length,
            },
            new DescriptorPoolSize()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)game.renderer.renderSwapChain.swapChainImages!.Length,
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
                MaxSets = (uint)game.renderer.renderSwapChain.swapChainImages!.Length,
            };

            if (game.vk!.CreateDescriptorPool(game.renderDevice.device, in poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }

        }
    }

    public void CreateDescriptorSets(Game game)
    {
        var layouts = new DescriptorSetLayout[game.renderer.renderSwapChain.swapChainImages!.Length];
        Array.Fill(layouts, game.graphicsPipeline.descriptorSetLayout);

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = (uint)game.renderer.renderSwapChain.swapChainImages!.Length,
                PSetLayouts = layoutsPtr,
            };

            descriptorSets = new DescriptorSet[game.renderer.renderSwapChain.swapChainImages.Length];
            fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
            {
                if (game.vk!.AllocateDescriptorSets(game.renderDevice.device, in allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }

        for (int i = 0; i < game.renderer.renderSwapChain.swapChainImages.Length; i++)
        {

            var cameraBufferInfo = Camera.GetDescriptorBufferInfo(game.graphicsPipeline.uniformBuffers![i]);

            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = game.renderImage.textureImageView,
                Sampler = game.imageSampler.textureSampler,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                Camera.GetWriteDescriptorSet(cameraBufferInfo,descriptorSets[i]),
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