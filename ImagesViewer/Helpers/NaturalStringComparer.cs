using System.Collections.Generic;
using System.Text.RegularExpressions;

public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var regex = new Regex(@"\d+|\D+");
        var xParts = regex.Matches(x);
        var yParts = regex.Matches(y);
        int count = Math.Min(xParts.Count, yParts.Count);

        for (int i = 0; i < count; i++)
        {
            string xPart = xParts[i].Value;
            string yPart = yParts[i].Value;

            if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
            {
                int numCompare = xNum.CompareTo(yNum);
                if (numCompare != 0)
                    return numCompare;
            }
            else
            {
                int strCompare = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                if (strCompare != 0)
                    return strCompare;
            }
        }
        return xParts.Count.CompareTo(yParts.Count);
    }
}
