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

public unsafe class RenderBuffer
{
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
            MemoryTypeIndex = game.FindMemoryType(memRequirements.MemoryTypeBits, properties),
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
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * game.vertices!.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        game.vk!.MapMemory(game.renderDevice.device, stagingBufferMemory, 0, bufferSize, 0, &data);
        game.vertices.AsSpan().CopyTo(new Span<Vertex>(data, game.vertices.Length));
        game.vk!.UnmapMemory(game.renderDevice.device, stagingBufferMemory);

        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref game.vertexBuffer, ref game.vertexBufferMemory);

        CopyBuffer(game, stagingBuffer, game.vertexBuffer, bufferSize);

        game.vk!.DestroyBuffer(game.renderDevice.device, stagingBuffer, null);
        game.vk!.FreeMemory(game.renderDevice.device, stagingBufferMemory, null);
    }

    public void CreateIndexBuffer(Game game)
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * game.indices!.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        game.vk!.MapMemory(game.renderDevice.device, stagingBufferMemory, 0, bufferSize, 0, &data);
        game.indices.AsSpan().CopyTo(new Span<uint>(data, game.indices.Length));
        game.vk!.UnmapMemory(game.renderDevice.device, stagingBufferMemory);

        CreateBuffer(game, bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref game.indexBuffer, ref game.indexBufferMemory);

        CopyBuffer(game, stagingBuffer, game.indexBuffer, bufferSize);

        game.vk!.DestroyBuffer(game.renderDevice.device, stagingBuffer, null);
        game.vk!.FreeMemory(game.renderDevice.device, stagingBufferMemory, null);
    }

    public void CreateUniformBuffers(Game game)
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        game.uniformBuffers = new Buffer[game.renderSwapChain.swapChainImages!.Length];
        game.uniformBuffersMemory = new DeviceMemory[game.renderSwapChain.swapChainImages!.Length];

        for (int i = 0; i < game.renderSwapChain.swapChainImages.Length; i++)
        {
            CreateBuffer(game, bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref game.uniformBuffers[i], ref game.uniformBuffersMemory[i]);
        }

    }

    public void CopyBuffer(Game game, Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer commandBuffer = game.BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            Size = size,
        };

        game.vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, in copyRegion);

        game.EndSingleTimeCommands(commandBuffer);
    }

    public void UpdateUniformBuffer(Game game, uint currentImage)
    {
        //Silk Window has timing information so we are skipping the time code.
        var time = (float)game.window!.Time;

        UniformBufferObject ubo = new()
        {
            model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(90.0f)),
            view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), (float)game.renderSwapChain.swapChainExtent.Width / game.renderSwapChain.swapChainExtent.Height, 0.1f, 10.0f),
        };
        ubo.proj.M22 *= -1;

        void* data;
        game.vk!.MapMemory(game.renderDevice.device, game.uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
        new Span<UniformBufferObject>(data, 1)[0] = ubo;
        game.vk!.UnmapMemory(game.renderDevice.device, game.uniformBuffersMemory![currentImage]);
    }
}