using ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;
using ImmersiveBuilding.Source.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelBehavior(CollectibleObject collectibleObject) : CustomToolModeBehavior(collectibleObject)
{
    private static readonly int lastModeIndex = Enum.GetValues(typeof(ShovelToolModes)).Cast<int>().Max();

    private List<SkillItem> modes = [];

    public override List<SkillItem> ToolModes => modes;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        // Init mode handlers
        modes = ObjectCacheUtil.GetOrCreate<List<SkillItem>>(
            api,
            "immersiveBuildingShovelModes",
            () =>

                [
                    new SkillItem() { Code = new AssetLocation("dig") },
                    new SkillItem()
                    {
                        Code = new AssetLocation("path"),
                        Data = new BuildingModeContext() { Handler = new ShovelPathModeHandler(api) },
                    },
                ]
        );

        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        // Init modes for client
        modes[0]
            .WithIcon(
                capi,
                capi.Gui.LoadSvgWithPadding(
                    loc: new AssetLocation("immersivebuilding:textures/icons/shovel-mode-dig.svg"),
                    textureWidth: 48,
                    textureHeight: 48,
                    padding: 8,
                    color: -1
                )
            )
            .Name = Lang.Get("Dig mode");
        modes[1]
            .WithIcon(
                capi,
                capi.Gui.LoadSvgWithPadding(
                    loc: new AssetLocation("immersivebuilding:textures/icons/shovel-mode-path.svg"),
                    textureWidth: 48,
                    textureHeight: 48,
                    padding: 8,
                    color: -1
                )
            )
            .Name = Lang.Get("Path mode");
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
        int selectedMode = slot.Itemstack.GetBuildingMode(modes);
        if (selectedMode <= 0 || selectedMode > lastModeIndex)
        {
            return false; // Not our mode
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
