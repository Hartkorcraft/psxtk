using Silk.NET.Vulkan;

public static class Utilities
{
    public static uint FindMemoryType(Game game, uint typeFilter, MemoryPropertyFlags properties)
    {
        game.vk!.GetPhysicalDeviceMemoryProperties(game.renderDevice.physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new Exception("failed to find suitable memory type!");
    }
}