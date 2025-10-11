using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.Utils;

public static class ToolModeUtils
{
    public const string BuildingModeAttributeName = "toolMode";

    public static int GetBuildingMode(this ItemStack itemStack, IEnumerable<SkillItem> toolModes)
    {
        string? selectedMode = itemStack.Attributes.GetString(BuildingModeAttributeName);
        if (selectedMode == null)
        {
            return 0;
        }

        int index = toolModes.IndexOf(mode => mode.Code == selectedMode);
        return index == -1 ? 0 : index;
    }

    public static void SetBuildingMode(this ItemStack itemStack, string value)
    {
        if (value == "game:default")
        {
            itemStack.Attributes.RemoveAttribute(BuildingModeAttributeName);
        }
        else
        {
            itemStack.Attributes.SetString(BuildingModeAttributeName, value);
        }
    }

    public static bool HasMode(this ItemStack itemStack, string toolModeCode) =>
        itemStack.Attributes.GetString(BuildingModeAttributeName) == toolModeCode;
}
