using ImmersiveBuilding.Shared;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Features.StoneItemModes;

public class StoneItemBehavior(CollectibleObject collectibleObject) : CollectibleBehavior(collectibleObject)
{
    private static readonly int lastModeIndex = Enum.GetValues(typeof(StoneItemBuildModes)).Cast<int>().Max();

    private SkillItem[] modes = [];
    private IModeHandler?[] modeHandlers = [];

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        // Init mode handlers
        modeHandlers = [null, new StoneItemModeHandler(api), null, null];

        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        // Init modes for client
        modes = ObjectCacheUtil.GetOrCreate(
            api,
            "immersiveBuildingStoneItemModes",
            () =>
                new SkillItem[]
                {
                    new SkillItem() { Code = new AssetLocation("default"), Name = Lang.Get("Default") }.WithIcon(
                        capi,
                        capi.Gui.LoadSvgWithPadding(
                            loc: new AssetLocation("immersivebuilding:textures/icons/shovel-mode-dig.svg"),
                            textureWidth: 48,
                            textureHeight: 48,
                            padding: 8,
                            color: -1
                        )
                    ),
                    new SkillItem() { Code = new AssetLocation("cobblestone"), Name = Lang.Get("Cobblestone") }.WithIcon(
                        capi,
                        capi.Gui.LoadSvgWithPadding(
                            loc: new AssetLocation("immersivebuilding:textures/icons/shovel-mode-dig.svg"),
                            textureWidth: 48,
                            textureHeight: 48,
                            padding: 8,
                            color: -1
                        )
                    ),
                    new SkillItem() { Code = new AssetLocation("cobblestoneslab"), Name = Lang.Get("Cobblestone slab") }.WithIcon(
                        capi,
                        capi.Gui.LoadSvgWithPadding(
                            loc: new AssetLocation("immersivebuilding:textures/icons/shovel-mode-path.svg"),
                            textureWidth: 48,
                            textureHeight: 48,
                            padding: 8,
                            color: -1
                        )
                    ),
                    new SkillItem() { Code = new AssetLocation("cobblestonestairs"), Name = Lang.Get("Cobblestone stairs") }.WithIcon(
                        capi,
                        capi.Gui.LoadSvgWithPadding(
                            loc: new AssetLocation("immersivebuilding:textures/icons/shovel-mode-path.svg"),
                            textureWidth: 48,
                            textureHeight: 48,
                            padding: 8,
                            color: -1
                        )
                    ),
                }
        );
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

        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
        modeHandlers[selectedMode]?.HandleStart(slot, byEntity, blockSel, entitySel);
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => modes;

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
    {
        return slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        slot.Itemstack.Attributes.SetInt(SharedConstants.ToolModeAttributeName, toolMode);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        for (int i = 0; i < modes.Length; i++)
        {
            modes[i].Dispose();
        }
    }

    private static bool CanHandleMode(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
    {
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
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
