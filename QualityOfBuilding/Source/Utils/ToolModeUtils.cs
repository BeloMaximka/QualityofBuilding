using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Utils;

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
            if (CompareWithoutAllocations(mode.Code, selectedMode))
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

    private static bool CompareWithoutAllocations(AssetLocation location, string codeWithDomain)
    {
        int domainLen = location.Domain.Length;
        int pathLen = location.Path.Length;

        if (codeWithDomain.Length != domainLen + pathLen + 1)
            return false;

        for (int i = 0; i < domainLen; i++)
        {
            if (codeWithDomain[i] != location.Domain[i])
                return false;
        }

        if (codeWithDomain[domainLen] != ':')
            return false;

        for (int i = 0; i < pathLen; i++)
        {
            if (codeWithDomain[domainLen + 1 + i] != location.Path[i])
                return false;
        }

        return true;
    }
}
