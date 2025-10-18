using QualityOfBuilding.Source.Gui;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes.Handbook;

public class HandbookBuildingRecipe
{
    public required ItemStack[] Tools { get; set; }
    public required ItemStack[][] IngredientGroups { get; set; }
}

public class BuildingItemHandbookInfoBehavior(CollectibleObject collectibleObject)
    : CollectibleBehavior(collectibleObject),
        ICustomHandbookPageContent
{
    private readonly ItemStack collectibleItemStack = new(collectibleObject);

    public List<ItemStack[]> IngredientForBlockGroups { get; set; } = [];

    public List<HandbookBuildingRecipe> RecipesToBuild { get; set; } = [];

    public void OnHandbookPageComposed(
        List<RichTextComponentBase> components,
        ItemSlot inSlot,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor
    )
    {
        AddIngredientForInfo(components, capi, openDetailPageFor);
        AddBuildByRecipesInfo(components, capi, openDetailPageFor);
    }

    private void AddIngredientForInfo(
        List<RichTextComponentBase> components,
        ICoreClientAPI capi,
        ActionConsumable<string> openDetailPageFor
    )
    {
        if (IngredientForBlockGroups.Count == 0)
        {
            return;
        }

        bool haveText = true;
        CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(
            components,
            capi,
            Lang.Get("handbook-ingredient-for-building"),
            ref haveText
        );

        foreach (ItemStack[] outputGroup in IngredientForBlockGroups)
        {
            SlideshowItemstackTextComponent itemStackComponent = new(
                capi,
                outputGroup,
                40,
                EnumFloat.Inline,
                (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))
            );
            components.Add(itemStackComponent);
        }
    }

    private void AddBuildByRecipesInfo(
        List<RichTextComponentBase> components,
        ICoreClientAPI capi,
        ActionConsumable<string> openDetailPageFor
    )
    {
        if (RecipesToBuild.Count == 0)
        {
            return;
        }

        bool haveText = true;
        CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, Lang.Get("Created by buiding"), ref haveText);

        foreach (HandbookBuildingRecipe recipe in RecipesToBuild)
        {
            if (recipe.Tools.Length > 0)
            {
                components.Add(
                    new SlideshowItemstackTextComponent(
                        capi,
                        recipe.Tools,
                        unscaledSize: 40,
                        EnumFloat.Inline,
                        (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))
                    )
                );
                components.Add(
                    new RichTextComponent(capi, "+", CairoFont.WhiteMediumText())
                    {
                        VerticalAlign = EnumVerticalAlign.Middle,
                        PaddingRight = 4,
                        PaddingLeft = 4,
                    }
                );
                components.Add(
                    new HotkeyComponent(capi, BuildingModeDialog.ToggleCombinationCode, CairoFont.WhiteSmallishText())
                    {
                        VerticalAlign = EnumVerticalAlign.Middle,
                        PaddingRight = 4,
                        PaddingLeft = 4,
                    }
                );
                components.Add(
                    new RichTextComponent(capi, ":", CairoFont.WhiteSmallishText())
                    {
                        VerticalAlign = EnumVerticalAlign.Middle,
                        PaddingLeft = 4,
                    }
                );
                components.Add(new ClearFloatTextComponent(capi, 0f));
            }

            foreach (var ingredientGroup in recipe.IngredientGroups.Where(group => group.Length > 0))
            {
                components.Add(
                    new SlideshowItemstackTextComponent(
                        capi,
                        ingredientGroup,
                        unscaledSize: 40,
                        EnumFloat.Inline,
                        (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))
                    )
                    {
                        ShowStackSize = true,
                    }
                );
            }

            components.Add(
                new RichTextComponent(capi, "=", CairoFont.WhiteMediumText())
                {
                    VerticalAlign = EnumVerticalAlign.Middle,
                    PaddingRight = 8,
                    PaddingLeft = 8,
                }
            );

            components.Add(
                new ItemstackTextComponent(
                    capi,
                    collectibleItemStack,
                    40,
                    0,
                    EnumFloat.Inline,
                    (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))
                )
            );

            components.Add(new ClearFloatTextComponent(capi, 8f));
        }
    }
}
