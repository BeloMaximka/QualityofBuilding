using QualityOfBuilding.Source.CollectibleBehaviors.BuildingModes;
using QualityOfBuilding.Source.Common;
using QualityOfBuilding.Source.Gui;
using QualityOfBuilding.Source.Utils;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace QualityOfBuilding.Source.CollectibleBehaviors.ShovelModes;

public class ShovelBehavior(CollectibleObject collectibleObject) : BuildingModeBehavior(collectibleObject)
{
    private const string stonePathCode = "stonepath-free";
    private readonly CollectibleObject collectibleObject = collectibleObject;
    private readonly List<BuildingMode> modes = [];

    public const string StonePathToolModeCode = $"{SharedConstants.ModName}:{stonePathCode}";
    public override List<BuildingMode> BuildingModes => modes;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        AddInitDefaultAndPathModes(api);
        AddSoilReplaceModes(api);
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
        if (!CanHandleMode(slot, byEntity, blockSel))
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
            return;
        }

        handHandling = EnumHandHandling.PreventDefault;
        handling = EnumHandling.PreventDefault;

        int selectedMode = slot.Itemstack.GetBuildingMode(modes);
        modes[selectedMode].Handler.HandleStart(slot, byEntity, blockSel, entitySel);
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

    private void AddInitDefaultAndPathModes(ICoreAPI api)
    {
        modes.Add(
            new BuildingMode()
            {
                Code = new AssetLocation("default"),
                Name = Lang.Get("default-behavior"),
                Handler = new DummyHanlder(),
                RenderSlot = new DummySlot(new(collectibleObject)),
            }
        );

        Block? stonePath = api.World.GetBlock(new AssetLocation(stonePathCode));
        Block? stonePathSlab = api.World.GetBlock(new AssetLocation("game:stonepathslab-free"));
        Block[] stonePathStairs =
        [
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-north-free")),
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-south-free")),
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-west-free")),
            api.World.GetBlock(new AssetLocation("stonepathstairs-up-east-free")),
        ];
        if (
            stonePath is null
            || stonePathSlab is null
            || stonePathStairs[0] is null
            || stonePathStairs[1] is null
            || stonePathStairs[2] is null
            || stonePathStairs[3] is null
        )
        {
            api.Logger.Warning("Some of stone path blocks was not found, skipping stone path building mode registration.");
            return;
        }
        ItemStack stonePathItem = new(stonePath);

        modes.Add(
            new BuildingMode()
            {
                Code = new AssetLocation(StonePathToolModeCode),
                Name = Lang.Get("make-roads"),
                Handler = new ShovelPathModeHandler(api, stonePath, stonePathSlab, stonePathStairs),
                Output = stonePathItem,
                RenderSlot = new DummySlot(stonePathItem),
                Ingredients =
                [
                    new()
                    {
                        Type = EnumItemClass.Item,
                        Code = "stone-*",
                        TranslatedName = Lang.Get("a-or-b", Lang.Get("any-stone"), stonePathItem?.GetName().ToLower()),
                        Quantity = 4,
                    },
                ],
            }
        );
    }

    private void AddSoilReplaceModes(ICoreAPI api)
    {
        // Maybe move hardcoded values into config
        string[] replacableBlocksCodes =
        [
            "terrainslabs:soil-*",
            "terrainslabs:forestfloor-*",
            "game:soil-*",
            "game:forestfloor-*",
            "drypackeddirt",
            "packeddirt",
            "rammed-light-*",
        ];
        List<int> replacableBlockIds = new(28);
        foreach (var code in replacableBlocksCodes)
        {
            Block[] blocks = api.World.SearchBlocks(code);
            replacableBlockIds.AddRange(blocks.Select(block => block.Id));
        }

        string[] outputBlockCodes =
        [
            "drypackeddirt",
            "packeddirt",
            "rammed-light-plain",
            "rammed-light-thickheavy",
            "rammed-light-thicklight",
            "rammed-light-thinheavy",
            "rammed-light-thinlight",
        ];
        int[] replacableBlockIdsArray = [.. replacableBlockIds];
        foreach (var code in outputBlockCodes)
        {
            Block? outputBlock = api.World.GetBlock(code);
            if (outputBlock is null)
            {
                continue;
            }
            ItemStack outputStack = new(outputBlock);
            BuildingMode replaceMode = new()
            {
                Code = new AssetLocation($"{SharedConstants.ModName}:{outputBlock.Code}"),
                Name = Lang.Get("replace-soil-with", outputStack.GetName().ToLower()),
                RenderSlot = new DummySlot(outputStack),
                Output = new(outputBlock),
                Handler = new ReplaceModeHandler(replacableBlockIdsArray, outputBlock.Id),
            };

            modes.Add(replaceMode);
        }
    }

    private bool CanHandleMode(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
    {
        if (slot.Itemstack.GetBuildingMode(modes) == 0)
        {
            return false;
        }

        if (blockSel == null)
        {
            return false;
        }

        IPlayer? byPlayer = (byEntity as EntityPlayer)?.Player;
        if (byPlayer == null)
        {
            return false;
        }

        if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        {
            return false;
        }

        return true;
    }
}
