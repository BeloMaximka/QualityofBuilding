using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace ImmersiveBuilding.Source.Utils;

public static class BuildingModeUtils
{
    public const string BuildingModeAttributeName = "buildingToolMode";

    public static int GetBuildingMode(this ItemStack itemStack)
    {
        return itemStack.Attributes.GetInt(BuildingModeAttributeName);
    }

    public static void SetBuildingMode(this ItemStack itemStack, int value)
    {
        if (value == 0)
        {
            itemStack.Attributes.RemoveAttribute(BuildingModeAttributeName);
        }
        else
        {
            itemStack.Attributes.SetInt(BuildingModeAttributeName, value);
        }
    }

    public static void UpdateStackAttributeIgnoreList()
    {
        if (!GlobalConstants.IgnoredStackAttributes.Any(attribute => attribute == BuildingModeAttributeName))
        {
            GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append(BuildingModeAttributeName);
        }
    }
}
