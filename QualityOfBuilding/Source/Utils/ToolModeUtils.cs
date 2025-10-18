using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Utils;

// I don't like additional allocations caused my AssetLocation
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

        int index = 0;
        foreach (SkillItem mode in toolModes)
        {
            if (mode.Code == selectedMode)
            {
                return index;
            }
            index++;
        }
        return 0;
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
