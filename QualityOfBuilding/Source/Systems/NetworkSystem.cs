using QualityOfBuilding.Source.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace QualityOfBuilding.Source.Systems;

internal class NetworkSystem : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Network.RegisterChannel(SetBuildingModePacket.Channel).RegisterMessageType(typeof(SetBuildingModePacket));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Network.RegisterChannel(SetBuildingModePacket.Channel)
            .RegisterMessageType(typeof(SetBuildingModePacket))
            .SetMessageHandler<SetBuildingModePacket>(SetBuildingModePacket.HandleServer);
    }
}
