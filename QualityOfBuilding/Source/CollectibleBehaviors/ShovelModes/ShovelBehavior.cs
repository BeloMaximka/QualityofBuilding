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

public class ShovelBehavior(CollectibleObject collectibleObject) : CustomToolModeBehavior(collectibleObject)
{
    private const string stonePathCode = "stonepath-free";

    private readonly CollectibleObject collectibleObject = collectibleObject;

    private readonly List<SkillItem> modes = [];

    public const string StonePathToolModeCode = $"{SharedConstants.ModName}:{stonePathCode}";

    public override List<SkillItem> ToolModes => modes;

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
        BuildingModeContext? context = modes[selectedMode].Data as BuildingModeContext;
        context?.Handler?.HandleStart(slot, byEntity, blockSel, entitySel);
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

    private void AddInitDefaultAndPathModes(ICoreAPI api)
    {
        Block? stonePath = api.World.GetBlock(new AssetLocation(stonePathCode));
        ItemStack? stonePathItem = stonePath is not null ? new(stonePath) : null;
        modes.Add(new SkillItem() { Code = new AssetLocation("default") });
        if (stonePath is not null)
        {
            modes.Add(
                new SkillItem()
                {
                    Code = new AssetLocation(StonePathToolModeCode),
                    Data = new BuildingModeContext()
                    {
                        Handler = new ShovelPathModeHandler(api, stonePath),
                        Output = stonePathItem,
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
                    },
                }
            );
        }

        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        // Init modes for client
        ItemStack itemStack = new(collectibleObject);
        modes[0].Name = Lang.Get("default-behavior");
        modes[0].RenderHandler = itemStack.GetRenderDelegate(capi);
        if (stonePathItem is not null)
        {
            modes[1].Name = Lang.Get("make-roads");
            modes[1].RenderHandler = stonePathItem.GetRenderDelegate(capi);
        }
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
            SkillItem replaceMode = new()
            {
                Code = new AssetLocation($"{SharedConstants.ModName}:{outputBlock.Code}"),
                Data = new BuildingModeContext()
                {
                    Handler = new ReplaceModeHandler(replacableBlockIdsArray, outputBlock.Id),
                    Output = new(outputBlock),
                },
            };

            if (api is ICoreClientAPI capi)
            {
                replaceMode.Name = Lang.Get("replace-soil-with", outputStack.GetName().ToLower());
                replaceMode.RenderHandler = outputStack.GetRenderDelegate(capi);
            }

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
