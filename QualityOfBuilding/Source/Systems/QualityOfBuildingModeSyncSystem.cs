using QualityOfBuilding.Source.Common;
using QualityOfBuilding.Source.Network;
using QualityOfBuilding.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace QualityOfBuilding.Source.Systems;

internal class QualityOfBuildingModeSyncSystem : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Network.RegisterChannel(SharedConstants.BuildingModeNetworkChannel).RegisterMessageType(typeof(SetBuildingModeMessage));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Network.RegisterChannel(SharedConstants.BuildingModeNetworkChannel)
            .RegisterMessageType(typeof(SetBuildingModeMessage))
            .SetMessageHandler<SetBuildingModeMessage>(OnBuildingModeChangeRequest);
    }

    // I hope it won't cause desyncs
    private static void OnBuildingModeChangeRequest(IPlayer fromPlayer, SetBuildingModeMessage networkMessage)
    {
        ItemStack? activeItem = fromPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (activeItem is null)
        {
            return;
        }

        activeItem.SetBuildingMode(networkMessage.ToolModeCode);
    }
}
