using ImmersiveBuilding.Source.Gui;
using ImmersiveBuilding.Source.Recipes;
using ImmersiveBuilding.Source.Utils;
using ImmersiveBuilding.Source.Utils.Inventory;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.ServerMods;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingItemBehavior : CustomToolModeBehavior
{
    private readonly List<SkillItem> modes;

    public override bool ClientSideOptional => true;

    public override List<SkillItem> ToolModes
    {
        get => modes;
    }

    public BuildingItemBehavior(CollectibleObject collectibleObject, ICoreAPI api, IEnumerable<SkillModeBuildingRecipe> recipes)
        : base(collectibleObject)
    {
        if (api is ICoreClientAPI clientAPI)
        {
            ClientAPI = clientAPI;
        }

        ItemStack itemStack = new(collectibleObject);
        modes = [new() { Code = collectibleObject.Code }];
        if (ClientAPI is not null)
        {
            modes[0].Name = itemStack.GetName();
            modes[0].RenderHandler = itemStack.GetRenderDelegate(ClientAPI);
        }

        foreach (SkillModeBuildingRecipe recipe in recipes)
        {
            ItemIngredient[] ingredients = recipe.GetItemIngredients();

            SkillItem mode = new()
            {
                Code = recipe.Code,
                Data = new BuildingModeContext()
                {
                    Output = recipe.Output.ResolvedItemStack,
                    Handler = new BuildingModeHandler(api) { Ingredients = ingredients, Output = recipe.Output.ResolvedItemStack },
                    Ingredients = ingredients,
                },
            };
            modes.Add(mode);

            if (ClientAPI is null)
            {
                continue;
            }
            if (recipe.Output.ResolvedItemStack is not null)
            {
                mode.Name = GetNameWithExtraInfo(recipe.Output.ResolvedItemStack);
                mode.RenderHandler = recipe.Output.ResolvedItemStack.GetRenderDelegate(ClientAPI);
                continue;
            }

            mode.Name = recipe.Output.Code;
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

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        int selectedMode = itemStack.GetBuildingMode(modes);
        if (selectedMode > 0 && modes[selectedMode].Data is BuildingModeContext context)
        {
            sb.Append($" ({context.Output?.GetName()})");
        }
    }

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
                EnumSlabPlaceMode.Horizontal => $"{itemStack.GetName()} ({Lang.Get("horizontal")})",
                EnumSlabPlaceMode.Vertical => $"{itemStack.GetName()} ({Lang.Get("vertical")})",
                _ => string.Empty,
            };
        }

        return itemStack.GetName();
    }
}
