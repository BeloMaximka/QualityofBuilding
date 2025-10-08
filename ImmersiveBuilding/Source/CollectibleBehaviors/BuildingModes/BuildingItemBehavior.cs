using ImmersiveBuilding.Source.Common;
using ImmersiveBuilding.Source.Gui;
using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingItemBehavior(CollectibleObject collectibleObject) : CollectibleBehavior(collectibleObject), ICustomHandbookPageContent
{
    private readonly CollectibleObject collectibleObject = collectibleObject;
    private int lastModeIndex = 0;
    private List<SkillItem> modes = [];
    private BuildingModeHandler?[] modeHandlers = [];
    private ICoreClientAPI? capi;

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
        if (api is not ICoreClientAPI clientAPI)
        {
            return;
        }
        capi = clientAPI;

        ItemStack itemStack = new(collectibleObject);
        modes = new(modeHandlers.Length)
        {
            new()
            {
                Code = collectibleObject.Code,
                Name = itemStack.GetName(),
                RenderHandler = GetItemRenderDelegate(capi, new DummySlot(itemStack)),
                Data = new BuildingModeContext() { Output = itemStack },
            },
        };

        for (int i = 1; i < modeHandlers.Length; i++)
        {
            if (modeHandlers[i]!.Output is null)
            {
                modes.Add(new SkillItem() { Code = modeHandlers[i]!.OutputCode, Name = modeHandlers[i]!.OutputCode });
                continue;
            }

            modes.Add(
                new SkillItem()
                {
                    Code = modeHandlers[i]!.Output!.Collectible.Code,
                    Name = GetNameWithExtraInfo(modeHandlers[i]!.Output!),
                    RenderHandler = GetBlockRenderDelegate(capi, modeHandlers[i]!.Output!),
                    Data = new BuildingModeContext() { Output = modeHandlers[i]?.Output, Ingredients = modeHandlers[i]!.Ingredients },
                }
            );
        }
    }

    #region Interactions
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
        int selectedMode = GetToolMode(slot);
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
        int selectedMode = GetToolMode(slot);
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
        int selectedMode = GetToolMode(slot);
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
        int selectedMode = GetToolMode(slot);
        if (selectedMode > 0 && selectedMode < lastModeIndex)
        {
            handled = EnumHandling.PreventSubsequent;
            return false;
        }
        return true;
    }
    #endregion

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        if (capi is null)
        {
            return base.GetHeldInteractionHelp(inSlot, ref handling);
        }

        return
        [
            new()
            {
                HotKeyCodes = [capi.Input.GetHotKeyByCode(BuildingModeDialog.ToggleCombinationCode).Code],
                ActionLangCode = "heldhelp-building-menu",
                MouseButton = EnumMouseButton.None
            },
        ];
    }

    public void OnHandbookPageComposed(
        List<RichTextComponentBase> components,
        ItemSlot inSlot,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor
    )
    {
        bool haveText = true;
        CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, Lang.Get("handbook-used-to-build"), ref haveText);

        foreach (var handler in modeHandlers)
        {
            if (handler?.Output is null)
            {
                continue;
            }

            ItemstackTextComponent itemStackComponent = new(
                capi,
                handler.Output,
                40,
                0,
                EnumFloat.Inline,
                (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))
            );
            components.Add(itemStackComponent);
        }
    }

    public ItemStack? GetSelectedBuildingOutput(ItemStack itemStack)
    {
        int selectedMode = GetToolMode(itemStack);
        if (selectedMode < modes.Count)
        {
            BuildingModeContext? context = modes[selectedMode]?.Data as BuildingModeContext;
            if (context is not null)
            {
                return context.Output;
            }
        }
        return null;
    }

    public void ToggleDialog(ItemSlot slot)
    {
        if (BuildingModeDialogSingleton.IsOpened())
        {
            BuildingModeDialogSingleton.TryClose();
            return;
        }

        if (capi is null || slot?.Itemstack is null)
        {
            return;
        }
        BuildingModeDialogSingleton.TryOpen(capi, slot.Itemstack, modes);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        for (int i = 0; i < modes.Count; i++)
        {
            modes[i].Dispose();
        }
    }

    private static string GetNameWithExtraInfo(ItemStack itemStack)
    {
        EnumSlabPlaceMode slabMode = (EnumSlabPlaceMode)itemStack.Attributes.GetInt("slabPlaceMode");
        if (slabMode != EnumSlabPlaceMode.Auto)
        {
            return slabMode switch
            {
                EnumSlabPlaceMode.Horizontal => $"{itemStack.GetName()} ({Lang.Get("Horizontal")})",
                EnumSlabPlaceMode.Vertical => $"{itemStack.GetName()} ({Lang.Get("Vertical")})",
                _ => string.Empty,
            };
        }

        return itemStack.GetName();
    }

    private static RenderSkillItemDelegate GetBlockRenderDelegate(ICoreClientAPI capi, ItemStack itemStack)
    {
        DummySlot dummySlot = new(itemStack);
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

    private static int GetToolMode(ItemSlot slot) => GetToolMode(slot.Itemstack);
    private static int GetToolMode(ItemStack itemStack) => itemStack.Attributes.GetInt(SharedConstants.BuildingModeAttributeName);
}
