using System.Text.RegularExpressions;

namespace ORM.Trace.Processing
{

    /// <summary>
    /// Normalizes SQL so equivalent statements can be grouped and compared.
    /// </summary>
    public static class SqlNormalizer
    {
        private static readonly Regex Parameters = new(@"@\w+", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Replaces parameter names, collapses whitespace, and normalizes casing.
        /// </summary>
        /// <param name="sql">The SQL statement to normalize.</param>
        /// <returns>The normalized statement, or an empty string for blank input.</returns>
        public static string Normalize(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))            
                return string.Empty;            

            sql = Parameters.Replace(sql, "?");
            sql = Whitespace.Replace(sql, " ");

            return sql.Trim().ToUpperInvariant();
        }
    }
}
