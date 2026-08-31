using System.Security.Cryptography;
using System.Text;

namespace ORM.Trace.Processing
{
    /// <summary>
    /// Creates stable identifiers for normalized SQL statements.
    /// </summary>
    public static class QueryFingerprintGenerator
    {
        /// <summary>
        /// Creates a SHA-256 fingerprint for normalized SQL.
        /// </summary>
        /// <param name="normalizedSql">The normalized SQL statement.</param>
        /// <returns>An uppercase hexadecimal SHA-256 hash.</returns>
        public static string Create(string normalizedSql)
        {
            var bytes = Encoding.UTF8.GetBytes(normalizedSql);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
