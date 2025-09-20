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

        string stoneName = collectibleObject.Code.Path.Split('-')[1]; // game:stone-chalk => chalk
        // Init mode handlers
        modeHandlers = [null, new StoneItemCobbleBlockModeHandler(api, stoneName), null, null];

        // Init modes for client
        if (api is not ICoreClientAPI capi)
        {
            return;
        }
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

    // These handling overrides are ugly but I haven't come up with a better solution yet
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
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
        if (selectedMode <= 0 || selectedMode > lastModeIndex)
        {
            return; // Not our mode
        }

        handHandling = EnumHandHandling.PreventDefault;
        handling = EnumHandling.PreventSubsequent;

        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (
            blockSel == null
            || byPlayer == null
            || !byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)
        )
        {
            return;
        }

        modeHandlers[selectedMode]?.HandleStart(slot, byEntity, blockSel, entitySel);
    }

    public override bool OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        ref EnumHandling handling
    )
    {
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
        if (selectedMode > 0 && selectedMode < lastModeIndex)
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return true;
    }

    public override void OnHeldInteractStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        ref EnumHandling handling
    )
    {
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
        if (selectedMode > 0 && selectedMode < lastModeIndex)
        {
            handling = EnumHandling.PreventSubsequent;
        }
    }

    public override bool OnHeldInteractCancel(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        EnumItemUseCancelReason cancelReason,
        ref EnumHandling handled
    )
    {
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.ToolModeAttributeName);
        if (selectedMode > 0 && selectedMode < lastModeIndex)
        {
            handled = EnumHandling.PreventSubsequent;
        }
        return true;
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
}
