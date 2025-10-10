using ImmersiveBuilding.Source.Gui;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.CollectibleBehaviors;

public abstract class CustomToolModeBehavior(CollectibleObject collectibleObject) : CollectibleBehavior(collectibleObject)
{
    public ICoreClientAPI? ClientAPI { get; set; }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        ClientAPI = api as ICoreClientAPI;
    }

    public abstract List<SkillItem> ToolModes { get; }

    public virtual void ToggleDialog(ItemSlot slot)
    {
        if (BuildingModeDialogSingleton.IsOpened())
        {
            BuildingModeDialogSingleton.TryClose();
            return;
        }

        if (ClientAPI is null || slot?.Itemstack is null)
        {
            return;
        }
        BuildingModeDialogSingleton.TryOpen(ClientAPI, slot.Itemstack, ToolModes);
    }
}
