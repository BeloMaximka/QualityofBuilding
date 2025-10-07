using ImmersiveBuilding.Source.Extensions;
using ImmersiveBuilding.Source.Extensions.Inventory;
using ImmersiveBuilding.Source.Recipes;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.BuildingModes;

public class BuildingModeHandler : IModeHandler
{
    private readonly ICoreAPI api;

    public readonly IReadOnlyCollection<ItemIngredient> Ingredients;

    public string OutputCode { get; private set; }

    public ItemStack? Output { get; private set; }

    public BuildingModeHandler(ICoreAPI api, SkillModeBuildingRecipe recipe, string wildcardValue)
    {
        this.api = api;
        Ingredients = recipe.GetItemIngredients(api.World, wildcardValue);
        OutputCode = recipe.ResolveSubstitute(recipe.Output.Code, wildcardValue);
        Block? block = api.World.GetBlock(OutputCode);
        if (block is not null)
        {
            Output = new(block);
            if (recipe.Output.Attributes is not null && new JsonObject(recipe.Output.Attributes).ToAttribute() is ITreeAttribute treeAttribute)
            {
                Output.Attributes.MergeTree(treeAttribute.ConvertLongsToInts());
            }
        }
    }

    public void HandleStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null || Output is null)
        {
            return;
        }

        BlockSelection newBlockSelection = blockSel;
        if (!api.World.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(Output.Block))
        {
            newBlockSelection = blockSel.AddPosCopy(blockSel.Face.Normali);
        }

        string resultCode = "success";
        Output.Block.CanPlaceBlock(api.World, byPlayer, newBlockSelection, ref resultCode);
        if (resultCode != "success")
        {
            if (byPlayer is IServerPlayer serverPlayer)
            {
                serverPlayer.SendIngameError(resultCode, Lang.Get($"placefailure-{resultCode}"));
            }
            return;
        }

        if (!byPlayer.TryTakeItems(Ingredients))
        {
            return;
        }

        // TryPlaceBlock instead of DoPlaceBlock because some blocks like BlockFence don't have DoPlaceBlock override
        Output.Block.TryPlaceBlock(api.World, byPlayer, Output, newBlockSelection, ref resultCode);
        UpdateNeighbours(newBlockSelection);
        api.World.PlaySoundAt(Output.Block.Sounds.Place, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
    }

    private void UpdateNeighbours(BlockSelection blockSelection) // Some manual stuff to make client update instant
    {
        BlockPos tempPos = blockSelection.Position.NorthCopy();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
        tempPos.Set(blockSelection.Position).South();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
        tempPos.Set(blockSelection.Position).West();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
        tempPos.Set(blockSelection.Position).East();
        api.World.BlockAccessor.GetBlock(tempPos).OnNeighbourBlockChange(api.World, tempPos, blockSelection.Position);
    }
}
