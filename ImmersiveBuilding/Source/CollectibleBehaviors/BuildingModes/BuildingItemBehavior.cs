using ImmersiveBuilding.Source.Gui;
using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Systems;
using ImmersiveBuilding.Source.Utils;
using ImmersiveBuilding.Source.Utils.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingItemBehavior(CollectibleObject collectibleObject)
    : CustomToolModeBehavior(collectibleObject),
        ICustomHandbookPageContent
{
    private readonly CollectibleObject collectibleObject = collectibleObject;
    private List<SkillItem> modes = [];

    public override List<SkillItem> ToolModes
    {
        get => modes;
    }

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

        // Init client part
        if (api is ICoreClientAPI clientAPI)
        {
            ClientAPI = clientAPI;
        }

        ItemStack itemStack = new(collectibleObject);
        modes = new(recipes.Length + 1) { new() { Code = collectibleObject.Code } };
        if (ClientAPI is not null)
        {
            modes[0].Name = itemStack.GetName();
            modes[0].RenderHandler = GetItemRenderDelegate(ClientAPI, new DummySlot(itemStack));
        }

        foreach (var recipe in recipes)
        {
            string wildcardValue = WildcardUtil.GetWildcardValue(recipe.Tool.Code, collectibleObject.Code);
            ItemIngredient[] ingredients = recipe.GetItemIngredients(api.World, wildcardValue);
            string outputCode = recipe.ResolveSubstitute(recipe.Output.Code, wildcardValue);
            Block? block = api.World.GetBlock(outputCode);
            ItemStack? output = null;

            if (block is not null)
            {
                output = new(block);
                if (
                    recipe.Output.Attributes is not null
                    && new JsonObject(recipe.Output.Attributes).ToAttribute() is ITreeAttribute treeAttribute
                )
                {
                    output.Attributes.MergeTree(treeAttribute.ConvertLongsToInts());
                }
            }

            SkillItem mode = new()
            {
                Code = outputCode,
                Data = new BuildingModeContext()
                {
                    Output = output,
                    Handler = new BuildingModeHandler(api) { Ingredients = ingredients, Output = output },
                    Ingredients = ingredients,
                },
            };
            modes.Add(mode);

            if (ClientAPI is null)
            {
                continue;
            }
            if (output is not null)
            {
                mode.Name = GetNameWithExtraInfo(output);
                mode.RenderHandler = GetBlockRenderDelegate(ClientAPI, output);
                continue;
            }

            mode.Name = outputCode;
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
        int selectedMode = slot.Itemstack.GetBuildingMode(modes);
        if (selectedMode > 0)
        {
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventSubsequent;
        }

        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (
            blockSel == null
            || byPlayer == null
            || !byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)
            || modes[selectedMode].Data is not BuildingModeContext context
        )
        {
            return;
        }

        context.Handler.HandleStart(slot, byEntity, blockSel, entitySel);
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
        if (slot.Itemstack.GetBuildingMode(modes) > 0)
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
        if (slot.Itemstack.GetBuildingMode(modes) > 0)
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
        if (slot.Itemstack.GetBuildingMode(modes) > 0)
        {
            handled = EnumHandling.PreventSubsequent;
            return false;
        }
        return true;
    }
    #endregion

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        if (ClientAPI is null)
        {
            return base.GetHeldInteractionHelp(inSlot, ref handling);
        }

        return
        [
            new()
            {
                HotKeyCodes = [ClientAPI.Input.GetHotKeyByCode(BuildingModeDialog.ToggleCombinationCode).Code],
                ActionLangCode = "heldhelp-building-menu",
                MouseButton = EnumMouseButton.None,
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

        foreach (var mode in modes)
        {
            if (mode.Data is not BuildingModeContext context || context.Output is null)
            {
                continue;
            }

            ItemstackTextComponent itemStackComponent = new(
                capi,
                context.Output,
                40,
                0,
                EnumFloat.Inline,
                (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))
            );
            components.Add(itemStackComponent);
        }
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
}
