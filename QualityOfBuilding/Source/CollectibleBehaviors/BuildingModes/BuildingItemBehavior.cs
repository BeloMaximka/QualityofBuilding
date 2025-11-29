using QualityOfBuilding.Source.Gui;
using QualityOfBuilding.Source.Recipes;
using QualityOfBuilding.Source.Utils;
using QualityOfBuilding.Source.Utils.Inventory;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.ServerMods;

namespace QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingItemBehavior : BuildingModeBehavior
{
    private readonly List<BuildingMode> modes = [];

    public override bool ClientSideOptional => true;

    public override List<BuildingMode> BuildingModes
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
        modes.Add(
            new()
            {
                Code = collectibleObject.Code,
                Name = itemStack.GetName(),
                RenderSlot = new DummySlot(itemStack),
                Handler = new ModeHandlerBase(),
            }
        );

        foreach (SkillModeBuildingRecipe recipe in recipes)
        {
            ItemIngredient[] ingredients = recipe.GetItemIngredients();
            BuildingMode option = new()
            {
                Code = recipe.Code,
                Ingredients = ingredients,
                Name = recipe.Output.Code,
                Handler = new BuildingModeHandler(api) { Ingredients = ingredients, Output = recipe.Output.ResolvedItemStack },
            };
            if (recipe.Output.ResolvedItemStack is not null)
            {
                option.Name = GetNameWithExtraInfo(recipe.Output.ResolvedItemStack);
                option.RenderSlot = new DummySlot(recipe.Output.ResolvedItemStack);
                option.Output = recipe.Output.ResolvedItemStack;
            }
            modes.Add(option);
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
        )
        {
            return;
        }

        modes[selectedMode].Handler.HandleStart(slot, byEntity, blockSel, entitySel);
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
        if (selectedMode > 0)
        {
            sb.Append($" ({modes[selectedMode].Output?.GetName()})");
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
