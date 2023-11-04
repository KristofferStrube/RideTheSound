using System.Globalization;

namespace RideTheSound.Extensions;

public static class DoubleExtensions
{
    public static string AsString(this double d) => Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
}
