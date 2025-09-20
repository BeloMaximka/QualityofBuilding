using ImmersiveBuilding.Shared;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace ImmersiveBuilding.Features.StoneItemModes;

public class StoneItemBehavior(CollectibleObject collectibleObject) : CollectibleBehavior(collectibleObject)
{
    private readonly CollectibleObject collectibleObject = collectibleObject;
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

        modes =
        [
            new()
            {
                Code = new AssetLocation("default"),
                Name = Lang.Get("Default"),
                RenderHandler = GetItemRenderDelegate(capi, new DummySlot(new ItemStack(collectibleObject))),
            },
            new SkillItem()
            {
                Code = new AssetLocation("cobblestone"),
                Name = Lang.Get("Cobblestone"),
                RenderHandler = GetBlockRenderDelegate(capi, collectibleObject.Code.Path.Replace("stone-", "cobblestone-")),
            },
            new SkillItem()
            {
                Code = new AssetLocation("cobblestoneslab"),
                Name = Lang.Get("Cobblestone slab"),
                RenderHandler = GetBlockRenderDelegate(
                    capi,
                    collectibleObject.Code.Path.Replace("stone-", "cobblestoneslab-") + "-down-free"
                ),
            },
            new SkillItem()
            {
                Code = new AssetLocation("cobblestonestairs"),
                Name = Lang.Get("Cobblestone stairs"),
                RenderHandler = GetBlockRenderDelegate(
                    capi,
                    collectibleObject.Code.Path.Replace("stone-", "cobblestonestairs-") + "-up-north-free"
                ),
            },
        ];
    }

    private static RenderSkillItemDelegate GetBlockRenderDelegate(ICoreClientAPI capi, string code)
    {
        Block block = capi.World.GetBlock(new AssetLocation(code));
        if (block == null)
        {
            return (code, dt, posX, posY) => { };
        }

        DummySlot dummySlot = new(new ItemStack(block));
        return GetItemRenderDelegate(capi, dummySlot);
    }

    private static RenderSkillItemDelegate GetItemRenderDelegate(ICoreClientAPI capi, ItemSlot slot)
    {
        return (code, dt, posX, posY) =>
        {
            double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGrid.unscaledSlotPadding;
            double scsize = GuiElement.scaled(size - 5);

            capi.Render.RenderItemstackToGui(
                slot,
                posX + scsize / 2,
                posY + scsize / 2,
                100,
                (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize),
                ColorUtil.WhiteArgb
            );
        };
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
