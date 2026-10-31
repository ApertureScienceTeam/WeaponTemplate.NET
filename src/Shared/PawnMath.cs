using System.Globalization;
using System.Text.RegularExpressions;

namespace Module;

internal static class PawnMath
{
    // AMXX floatround_round: floor((double)value + .5), including negative ties.
    // https://github.com/alliedmodders/amxmodx/blob/master/amxmodx/float.cpp#L198
    public static int Round(float value) => (int)Math.Floor((double)value + 0.5);

    private static readonly Regex IntegerPrefix = new(@"^\s*[+-]?[0-9]+");
    private static readonly Regex FloatPrefix = new(@"^\s*[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?");

    // Pawn str_to_num/str_to_float accept a leading number followed by a suffix/comment.
    public static int ParseInt(string value) => int.TryParse(IntegerPrefix.Match(value).Value,
        NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;

    public static float ParseFloat(string value) => float.TryParse(FloatPrefix.Match(value).Value,
        NumberStyles.Float, CultureInfo.InvariantCulture, out float result) && float.IsFinite(result) ? result : 0;
}
