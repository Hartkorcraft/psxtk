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

public unsafe class ImageSampler
{
    public Sampler textureSampler;

    public void Destroy(Game game)
    {
        game.vk!.DestroySampler(game.renderDevice.device, textureSampler, null);
    }

    public void CreateTextureSampler(Game game)
    {
        game.vk!.GetPhysicalDeviceProperties(game.renderDevice.physicalDevice, out PhysicalDeviceProperties properties);

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MinLod = 0,
            MaxLod = game.renderImage.mipLevels,
            MipLodBias = 0,
        };

        fixed (Sampler* textureSamplerPtr = &textureSampler)
        {
            if (game.vk!.CreateSampler(game.renderDevice.device, in samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }
}