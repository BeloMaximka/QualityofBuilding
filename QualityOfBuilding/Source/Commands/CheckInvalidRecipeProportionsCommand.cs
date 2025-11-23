using QualityOfBuilding.Source.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Commands;

public static class CheckInvalidRecipeProportionsCommand
{
    public static void Register(ICoreClientAPI api)
    {
        api.ChatCommands.GetOrCreate("qob").BeginSubCommand("recipes").BeginSubCommand("checkproportions").HandleWith(CheckProportions);
    }

    private static TextCommandResult CheckProportions(TextCommandCallingArgs args)
    {
        try
        {
            ICoreAPI api = args.Caller.Entity.Api;
            HashSet<string> blockWithInvalidIngredientProportions = [];
            foreach (
                var outputCode in api
                    .ModLoader.GetModSystem<MainSystem>()
                    .BuildingRecipes.Select(buildingRecipe => buildingRecipe.Output.Code)
            )
            {
                var gridRecipe = api.World.GridRecipes.FirstOrDefault(gr => gr.Output.Code == outputCode);
                if (gridRecipe is null)
                {
                    continue;
                }

                foreach (
                    var ingredientGroup in gridRecipe
                        .resolvedIngredients.Where(ingredient => ingredient is not null && !ingredient.IsTool)
                        .GroupBy(ingredient => ingredient.PatternCode)
                )
                {
                    if (ingredientGroup.Count() * ingredientGroup.First().Quantity / gridRecipe.Output.Quantity == 0)
                    {
                        blockWithInvalidIngredientProportions.Add(outputCode.ToString());
                        break;
                    }
                }
            }
            string response =
                blockWithInvalidIngredientProportions.Count == 0
                    ? "All good."
                    : $"Found {blockWithInvalidIngredientProportions.Count} recipes with incorrect ingredient proportions:\n"
                        + string.Join("\n", blockWithInvalidIngredientProportions);

            return new TextCommandResult() { Status = EnumCommandStatus.Success, StatusMessage = response };
        }
        catch (Exception e)
        {
            return new TextCommandResult() { Status = EnumCommandStatus.Error, StatusMessage = e.ToString() };
        }
    }
}
