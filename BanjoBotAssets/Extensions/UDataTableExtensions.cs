namespace BanjoBotAssets.Extensions
{
    internal static class UDataTableExtensions
    {
        public static Dictionary<string, FStructFallback> ToDictionary(this UDataTable table)
        {
            var dict = new Dictionary<string, FStructFallback>(table.RowMap.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var (k, v) in table.RowMap)
                dict.Add(k.Text, v);

            return dict;
        }
    }
}
