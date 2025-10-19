using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using QualityOfBuilding.Source.Recipes;
using QualityOfBuilding.Source.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace QualityOfBuilding.Source.Commands;

// I don't care how unreadable this is
static class GenerateBuildingRecipeJsonFromGridRecipeCommand
{
    private static readonly JsonSerializerSettings serializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new StringEnumConverter(new CamelCaseNamingStrategy(), allowIntegerValues: false) },
        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true,
            },
        },
    };

    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands.GetOrCreate("qob")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("recipes")
            .BeginSubCommand("generate")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Word("blockCode"), api.ChatCommands.Parsers.All("toolCode"))
            .HandleWith(SendRecipesInChat);
    }

    private static TextCommandResult SendRecipesInChat(TextCommandCallingArgs args)
    {
        try
        {
            List<string> jsons = [];
            AssetLocation blockCode = new(args.Parsers[0].GetValue() as string);
            AssetLocation toolCode = new(args.Parsers[1].GetValue() as string);

            ICoreAPI api = args.Caller.Entity.Api;
            Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Logger, "recipes/grid");

            foreach ((AssetLocation location, JToken content) in files)
            {
                if (content is JObject)
                {
                    TryConvertAndAdd(content.ToObject<GridRecipe>(location.Domain), blockCode, toolCode, jsons);
                }
                else if (content is JArray array)
                {
                    foreach (JToken token in array)
                    {
                        TryConvertAndAdd(token.ToObject<GridRecipe>(location.Domain), blockCode, toolCode, jsons);
                    }
                }
            }

            string spacer = "===========================";
            return new TextCommandResult()
            {
                Status = EnumCommandStatus.Success,
                StatusMessage = spacer + "\n" + string.Join(",\n", jsons),
            };
        }
        catch (Exception e)
        {
            return new TextCommandResult() { Status = EnumCommandStatus.Error, StatusMessage = e.ToString() };
        }
    }

    private static void TryConvertAndAdd(GridRecipe recipe, AssetLocation blockCode, AssetLocation toolCode, List<string> jsons)
    {
        if (!WildcardUtil.Match(blockCode, recipe.Output.Code))
        {
            return;
        }
        var tool = recipe.Ingredients.FirstOrDefault(ingredient => WildcardUtil.Match(toolCode, ingredient.Value.Code));
        if (tool.Value is null)
        {
            return;
        }
        tool.Value.Name ??= tool.Value.Code.FirstCodePart();

        SkillModeBuildingRecipeJson buildingRecipe = new()
        {
            CodeSuffix = null!,
            Output = new() { Code = recipe.Output.Code, Type = recipe.Output.Type },
            Tool = new()
            {
                Code = tool.Value.Code,
                Type = tool.Value.Type,
                AllowVariants = tool.Value.AllowedVariants,
                SkipVariants = tool.Value.SkipVariants,
                Name = tool.Value.Name,
            },
            Ingredients =
            [
                .. recipe.Ingredients.Select(ingredient => new SkillModeRecipeIngredient()
                {
                    Code =
                        ingredient.Value == tool.Value
                            ? ingredient.Value.Code.WithReplacedWildcard($"{{{tool.Value.Name}}}")
                            : ingredient.Value.Code,
                    Type = ingredient.Value.Type,
                    Quantity = ingredient.Value.Quantity * recipe.IngredientPattern.CountChars(ingredient.Key[0]) / recipe.Output.Quantity,
                }),
            ],
        };

        jsons.Add(JsonConvert.SerializeObject(buildingRecipe, serializerSettings));
        return;
    }
}
