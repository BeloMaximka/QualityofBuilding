using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Extensions;

public static class AssetLocationExtensions
{
    private static readonly Regex stateVariationsRegex = new(
        pattern: "-up|-down|-free|-snow|-north|-south|-west|-east|-ns|-we|-ud",
        options: RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public static AssetLocation WithReplacedWildcard(this AssetLocation location, string wildcardValue)
    {
        if (!location.IsWildCard)
        {
            return location;
        }

        return new AssetLocation(location.ToString().Replace("*", wildcardValue));
    }

    /// <summary>
    ///  Return mutated original with parts that represent block states rather than material<br></br>
    /// </summary>
    /// <param name="location"></param>
    /// <returns>cobblestonestairs-chalk-up-north-free => cobblestonestairs-chalk-*-*</returns>
    public static AssetLocation WithStatePartsAsWildcards(this AssetLocation location)
    {
        location.Path = stateVariationsRegex.Replace(location.Path, "-*");
        return location;
    }
}
