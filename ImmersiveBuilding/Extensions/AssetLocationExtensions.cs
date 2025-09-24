using Vintagestory.API.Common;

namespace ImmersiveBuilding.Extensions;
public static class AssetLocationExtensions
{
    public static AssetLocation WithReplacedWildcard(this AssetLocation location, string wildcardValue)
    {
        if (!location.IsWildCard)
        {
            return location;
        }

        return new AssetLocation(location.ToString().Replace("*", wildcardValue));
    }
}
