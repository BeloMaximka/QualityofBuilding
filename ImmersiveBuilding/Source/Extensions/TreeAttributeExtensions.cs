using Vintagestory.API.Datastructures;

namespace ImmersiveBuilding.Source.Extensions;

public static class TreeAttributeExtensions
{
    public static ITreeAttribute ConvertLongsToInts(this ITreeAttribute treeAttribute)
    {
        foreach (var item in treeAttribute)
        {
            if (item.Value is ScalarAttribute<long> longAttribute)
            {
                treeAttribute.SetInt(item.Key, (int)longAttribute.value);
            }
        }
        return treeAttribute;
    }
}
