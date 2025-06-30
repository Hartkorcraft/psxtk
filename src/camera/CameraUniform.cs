
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe struct CameraUniform
{
    public Matrix4X4<float> model;
    public Matrix4X4<float> view;
    public Matrix4X4<float> proj;


    public static DescriptorBufferInfo GetDescriptorBufferInfo(Buffer buffer)
    {
        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = buffer,
            Offset = 0,
            Range = (ulong)Unsafe.SizeOf<CameraUniform>(),

        };
        return bufferInfo;
    }

    public static WriteDescriptorSet GetWriteDescriptorSet(DescriptorBufferInfo bufferInfo, DescriptorSet descriptorSet)
    {
        return new WriteDescriptorSet()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = descriptorSet,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo,
        };
    }
}
