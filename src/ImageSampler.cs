using Silk.NET.Vulkan;

public unsafe class ImageSampler
{
    public Sampler textureSampler;

    public void Destroy(Game game)
    {
        game.vk!.DestroySampler(game.renderDevice.device, textureSampler, null);
    }

    public void Init(Game game)
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