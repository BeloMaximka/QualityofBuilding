using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.Common;
using ImmersiveBuilding.Source.Utils;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelBehavior(CollectibleObject collectibleObject) : CustomToolModeBehavior(collectibleObject)
{
    private const string stonePathCode = "stonepath-free";

    private readonly CollectibleObject collectibleObject = collectibleObject;

    private readonly List<SkillItem> modes = [];

    public const string StonePathToolModeCode = $"{SharedConstants.ModName}:{stonePathCode}";

    public override List<SkillItem> ToolModes => modes;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        Block? stonePath;
        stonePath = api.World.GetBlock(new AssetLocation(stonePathCode));
        ItemStack? stonePathItem = stonePath is not null ? new(stonePath) : null;

        // Init mode handlers
        modes.Add(new SkillItem() { Code = new AssetLocation("default") });
        if (stonePath is not null)
        {
            modes.Add(
                new SkillItem()
                {
                    Code = new AssetLocation(StonePathToolModeCode),
                    Data = new BuildingModeContext() { Handler = new ShovelPathModeHandler(api, stonePath), Output = stonePathItem },
                }
            );
        }

        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        // Init modes for client
        ItemStack itemStack = new(collectibleObject);
        modes[0].Name = itemStack.GetName();
        modes[0].RenderHandler = itemStack.GetRenderDelegate(capi);
        if (stonePathItem is not null)
        {
            modes[1].Name = stonePathItem.GetName();
            modes[1].RenderHandler = stonePathItem.GetRenderDelegate(capi);
        }
    }

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handHandling,
        ref EnumHandling handling
    )
    {
        if (!CanHandleMode(slot, byEntity, blockSel))
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
            return;
        }

        handHandling = EnumHandHandling.PreventDefault;
        handling = EnumHandling.PreventDefault;

        int selectedMode = slot.Itemstack.GetBuildingMode(modes);
        BuildingModeContext? context = modes[selectedMode].Data as BuildingModeContext;
        context?.Handler?.HandleStart(slot, byEntity, blockSel, entitySel);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        for (int i = 0; i < modes.Count; i++)
        {
            modes[i].Dispose();
        }
    }

    private bool CanHandleMode(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
    {
        if (slot.Itemstack.GetBuildingMode(modes) == 0)
        {
            return false;
        }

        if (blockSel == null)
        {
            return false;
        }

        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return false;
        }

        if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        {
            return false;
        }

        return true;
    }
}
