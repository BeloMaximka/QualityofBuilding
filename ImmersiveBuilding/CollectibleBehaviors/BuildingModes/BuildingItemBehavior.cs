using ImmersiveBuilding.Common;
using ImmersiveBuilding.Recipes;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.CollectibleBehaviors.BuildingModes;

internal class BuildingItemBehavior(CollectibleObject collectibleObject) : CollectibleBehavior(collectibleObject)
{
    private readonly CollectibleObject collectibleObject = collectibleObject;
    private int lastModeIndex = 0;

    private SkillItem[] modes = [];
    private IModeHandler?[] modeHandlers = [];

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        // Init mode handlers
        IEnumerable<SkillModeBuildingRecipe> recipes = api
            .ModLoader.GetModSystem<ImmersiveBuildingModSystem>()
            .BuildingRecipes.Where(recipe =>
                WildcardUtil.Match(recipe.Tool.Code, collectibleObject.Code)
                && (
                    recipe.Tool.SkipVariants is null
                    || !recipe.Tool.SkipVariants.Contains(WildcardUtil.GetWildcardValue(recipe.Tool.Code, collectibleObject.Code))
                )
            ); // TODO: optimize this

        modeHandlers =
        [
            null, // handler for default mode (vanilla behavior)
            .. recipes.Select(
                (recipe) =>
                {
                    return new BuildingModeHandler(api, recipe, WildcardUtil.GetWildcardValue(recipe.Tool.Code, collectibleObject.Code));
                }
            ),
        ];
        lastModeIndex = modeHandlers.Length - 1;

        // Init modes for client
        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        ItemStack itemStack = new(collectibleObject);
        modes =
        [
            new()
            {
                Code = collectibleObject.Code,
                Name = itemStack.GetName(),
                RenderHandler = GetItemRenderDelegate(capi, new DummySlot(itemStack)),
            },
            .. recipes.Select((recipe) => GetModeFromRecipe(capi, recipe)),
        ];
    }

    private SkillItem GetModeFromRecipe(ICoreClientAPI capi, SkillModeBuildingRecipe recipe)
    {
        string blockCode = recipe.ResolveSubstitute(
            recipe.Output.Code,
            WildcardUtil.GetWildcardValue(recipe.Tool.Code, collectibleObject.Code)
        );
        Block? block = capi.World.GetBlock(blockCode);
        if (block == null)
        {
            return new SkillItem() { Code = blockCode, Name = blockCode };
        }

        return new SkillItem()
        {
            Code = block.Code,
            Name = new ItemStack(block).GetName(),
            RenderHandler = GetBlockRenderDelegate(capi, block),
        };
    }

    private static RenderSkillItemDelegate GetBlockRenderDelegate(ICoreClientAPI capi, Block block)
    {
        DummySlot dummySlot = new(new ItemStack(block));
        return GetItemRenderDelegate(capi, dummySlot);
    }

    private static RenderSkillItemDelegate GetItemRenderDelegate(ICoreClientAPI capi, ItemSlot slot)
    {
        return (code, dt, posX, posY) =>
        {
            double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
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
