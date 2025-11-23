using ProtoBuf;
using QualityOfBuilding.Source.Utils;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Network;

[ProtoContract]
public class SetBuildingModePacket
{
    public const string Channel = "QobBuildingToolMode";

    [ProtoMember(1)]
    public required string ToolModeCode { get; set; }

    public static void HandleServer(IPlayer fromPlayer, SetBuildingModePacket networkMessage)
    {
        ItemStack? activeItem = fromPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (activeItem is null)
        {
            return;
        }

        activeItem.SetBuildingMode(networkMessage.ToolModeCode);
    }
}
