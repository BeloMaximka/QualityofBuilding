using ImmersiveBuilding.Source.Common;
using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Systems;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingItemBehavior(CollectibleObject collectibleObject) : CollectibleBehavior(collectibleObject)
{
    private const int itemsPerColumn = 8;

    private readonly CollectibleObject collectibleObject = collectibleObject;
    private ImmersiveBuildingRenderingSystem? renderingSystem;
    private int lastModeIndex = 0;
    private SkillItem[] modes = [];
    private BuildingModeHandler?[] modeHandlers = [];
    private ItemStack[] itemsToRender = [];
    private bool differenModesOpened = false; // to handle other behaviors like CollectibleBehaviorArtPigment

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        // Init mode handlers
        SkillModeBuildingRecipe[] recipes =
        [
            .. api
                .ModLoader.GetModSystem<ImmersiveBuildingModSystem>()
                .BuildingRecipes.Where(recipe =>
                    WildcardUtil.Match(recipe.Tool.Code, collectibleObject.Code)
                    && (
                        recipe.Tool.AllowVariants.Length == 0
                        || recipe.Tool.AllowVariants.Contains(WildcardUtil.GetWildcardValue(recipe.Tool.Code, collectibleObject.Code))
                    )
                ),
        ]; // TODO: optimize this

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

        // Init client part
        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        // Init things to render the tooltip at the top
        ItemStack itemStack = new(collectibleObject);
        Block airBlock = capi.World.GetBlock(0);
        renderingSystem = api.ModLoader.GetModSystem<ImmersiveBuildingRenderingSystem>();
        itemsToRender = [.. modeHandlers.Select(handler => new ItemStack(handler?.Block ?? airBlock))];
        itemsToRender[0] = itemStack;

        modes = new SkillItem[recipes.Length + 1];
        modes[0] = new()
        {
            Code = collectibleObject.Code,
            Name = itemStack.GetName(),
            RenderHandler = GetItemRenderDelegate(capi, new DummySlot(itemStack)),
        };
        for (int i = 1; i < modes.Length; i++)
        {
            modes[i] = GetModeFromRecipe(capi, recipes[i - 1], i % itemsPerColumn == 0);
        }
    }

    private SkillItem GetModeFromRecipe(ICoreClientAPI capi, SkillModeBuildingRecipe recipe, bool linebreak)
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
            Linebreak = linebreak,
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
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.SkillBuldingModeAttributeName);
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
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.SkillBuldingModeAttributeName);
        if (selectedMode > 0 && selectedMode < lastModeIndex)
        {
            handling = EnumHandling.PreventSubsequent;
            return false;
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
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.SkillBuldingModeAttributeName);
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
        int selectedMode = slot.Itemstack.Attributes.GetInt(SharedConstants.SkillBuldingModeAttributeName);
        if (selectedMode > 0 && selectedMode < lastModeIndex)
        {
            handled = EnumHandling.PreventSubsequent;
            return false;
        }
        return true;
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        differenModesOpened = false;
        if (!forPlayer.Entity.Controls.ShiftKey)
        {
            return modes;
        }

        foreach (CollectibleBehavior behavior in collectibleObject.CollectibleBehaviors)
        {
            if (behavior is BuildingItemBehavior)
            {
                continue;
            }

            SkillItem[] otherModes = behavior.GetToolModes(slot, forPlayer, blockSel);
            if (otherModes?.Length > 0)
            {
                differenModesOpened = true;
                return otherModes;
            }
        }
        return modes;
    }

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
    {
        int index = slot.Itemstack.Attributes.GetInt(SharedConstants.SkillBuldingModeAttributeName);

        if (renderingSystem is not null && !renderingSystem.SkillModeHud.IsOpened() && index < itemsToRender.Length)
        {
            renderingSystem.SkillModeHud.Item = itemsToRender[index];
            renderingSystem.SkillModeHud.TryOpen(false);
        }

        return index;
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        if (differenModesOpened)
        {
            return;
        }

        if (renderingSystem is not null && toolMode < itemsToRender.Length)
        {
            renderingSystem.SkillModeHud.Item = itemsToRender[toolMode];
        }
        slot.Itemstack.Attributes.SetInt(SharedConstants.SkillBuldingModeAttributeName, toolMode);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        for (int i = 0; i < modes.Length; i++)
        {
            modes[i].Dispose();
        }
    }
}
