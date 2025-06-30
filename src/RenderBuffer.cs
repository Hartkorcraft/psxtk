using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

public unsafe class RenderBuffer
{
    public DeviceMemory vertexBufferMemory;
    public DeviceMemory indexBufferMemory;

    public void CreateBuffer(Game game, ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer)
        {
            if (game.vk!.CreateBuffer(game.renderDevice.device, in bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        MemoryRequirements memRequirements = new();
        game.vk!.GetBufferMemoryRequirements(game.renderDevice.device, buffer, out memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = Utilities.FindMemoryType(game, memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (game.vk!.AllocateMemory(game.renderDevice.device, in allocateInfo, null, bufferMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        game.vk!.BindBufferMemory(game.renderDevice.device, buffer, bufferMemory, 0);
    }

    public void CreateVertexBuffer(Game game)
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * game.model.vertices!.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        game.vk!.MapMemory(game.renderDevice.device, stagingBufferMemory, 0, bufferSize, 0, &data);
        game.model.vertices.AsSpan().CopyTo(new Span<Vertex>(data, game.model.vertices.Length));
        game.vk!.UnmapMemory(game.renderDevice.device, stagingBufferMemory);

        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref game.vertexBuffer, ref vertexBufferMemory);

        CopyBuffer(game, stagingBuffer, game.vertexBuffer, bufferSize);

        game.vk!.DestroyBuffer(game.renderDevice.device, stagingBuffer, null);
        game.vk!.FreeMemory(game.renderDevice.device, stagingBufferMemory, null);
    }

    public void CreateIndexBuffer(Game game)
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * game.model.indices!.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        game.vk!.MapMemory(game.renderDevice.device, stagingBufferMemory, 0, bufferSize, 0, &data);
        game.model.indices.AsSpan().CopyTo(new Span<uint>(data, game.model.indices.Length));
        game.vk!.UnmapMemory(game.renderDevice.device, stagingBufferMemory);

        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref game.indexBuffer, ref indexBufferMemory);

        CopyBuffer(game, stagingBuffer, game.indexBuffer, bufferSize);

        game.vk!.DestroyBuffer(game.renderDevice.device, stagingBuffer, null);
        game.vk!.FreeMemory(game.renderDevice.device, stagingBufferMemory, null);
    }

    public void CreateUniformBuffers(Game game)
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<CameraUniform>();

        game.graphicsPipeline.uniformBuffers = new Buffer[game.renderer.renderSwapChain.swapChainImages!.Length];
        game.graphicsPipeline.uniformBuffersMemory = new DeviceMemory[game.renderer.renderSwapChain.swapChainImages!.Length];

        for (int i = 0; i < game.renderer.renderSwapChain.swapChainImages.Length; i++)
        {
            CreateBuffer(game, bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref game.graphicsPipeline.uniformBuffers[i], ref game.graphicsPipeline.uniformBuffersMemory[i]);
        }

    }

    public void CopyBuffer(Game game, Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        var commandBuffer = game.renderer.commands.BeginSingleTimeCommands(game);

        BufferCopy copyRegion = new()
        {
            Size = size,
        };

        game.vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, in copyRegion);

        game.renderer.commands.EndSingleTimeCommands(game, commandBuffer);
    }

    public void UpdateUniformBuffer(Game game, Camera camera, uint currentImage)
    {
        //Silk Window has timing information so we are skipping the time code.
        var time = (float)game.gameWindow.window!.Time;


        CameraUniform cameraUniform = new()
        {
            model = Matrix4X4<float>.Identity,
            view = Matrix4X4.CreateLookAt(camera.CameraPosition.To3D(), (camera.CameraPosition + camera.CameraFront).To3D(), camera.CameraUp.To3D()),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(camera.CameraZoom), (float)game.renderer.renderSwapChain.swapChainExtent.Width / game.renderer.renderSwapChain.swapChainExtent.Height, 0.1f, 100.0f)
        };
        cameraUniform.proj.M22 *= -1;

        void* data;
        game.vk!.MapMemory(game.renderDevice.device, game.graphicsPipeline.uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<CameraUniform>(), 0, &data);
        new Span<CameraUniform>(data, 1)[0] = cameraUniform;
        game.vk!.UnmapMemory(game.renderDevice.device, game.graphicsPipeline.uniformBuffersMemory![currentImage]);
    }

    public void CopyBufferToImage(Game game, Buffer buffer, Image image, uint width, uint height)
    {
        var commandBuffer = game.renderer.commands.BeginSingleTimeCommands(game);

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(width, height, 1),

        };

        game.vk!.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, in region);

        game.renderer.commands.EndSingleTimeCommands(game, commandBuffer);
    }

}