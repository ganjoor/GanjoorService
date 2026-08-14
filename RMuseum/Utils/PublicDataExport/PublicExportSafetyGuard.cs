using RMuseum.Models.Ganjoor.PublicExport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RMuseum.Utils.PublicDataExport
{
    /// <summary>
    /// Belt-and-suspenders check on top of the allowlist design: even though the export DTOs
    /// only ever declare the fields we explicitly want published, this scans every DTO type in
    /// the RMuseum.Models.Ganjoor.PublicExport namespace by reflection and throws if a property
    /// name or type looks like it could carry personal data. Call this once at application
    /// startup (Development/CI) and/or from a test — see AssertSafe().
    /// </summary>
    public static class PublicExportSafetyGuard
    {
        /// <summary>
        /// property-name patterns that must never appear on an export DTO
        /// </summary>
        private static readonly Regex[] _forbiddenNamePatterns = new[]
        {
            new Regex("UserId", RegexOptions.IgnoreCase),
            new Regex("OwnerId", RegexOptions.IgnoreCase),
            new Regex("ReviewerId", RegexOptions.IgnoreCase),
            new Regex("^Email$", RegexOptions.IgnoreCase),
            new Regex("Email$", RegexOptions.IgnoreCase),
            new Regex("^Ip$", RegexOptions.IgnoreCase),
            new Regex("IpAddress", RegexOptions.IgnoreCase),
            new Regex("Password", RegexOptions.IgnoreCase),
            new Regex("Token", RegexOptions.IgnoreCase),
            new Regex("PhoneNumber", RegexOptions.IgnoreCase),
            new Regex("^AuthorName$", RegexOptions.IgnoreCase),
            new Regex("^AuthorUrl$", RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// Scans every public class in the PublicExport DTO namespace. Throws InvalidOperationException
        /// naming the offending type/property if anything trips the checks.
        /// </summary>
        public static void AssertSafe()
        {
            var dtoTypes = typeof(PoemPublicDto).Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.Namespace == typeof(PoemPublicDto).Namespace)
                .ToList();

            var violations = new List<string>();

            foreach (var type in dtoTypes)
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (_forbiddenNamePatterns.Any(p => p.IsMatch(prop.Name)))
                    {
                        violations.Add($"{type.Name}.{prop.Name} matches a forbidden field-name pattern");
                    }

                    // a bare Guid property on a public export DTO is almost always an entity/user
                    // reference (our public ids are all ints); flag it for manual review
                    if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                    {
                        violations.Add($"{type.Name}.{prop.Name} is a Guid — likely an internal entity/user reference, not public data");
                    }
                }
            }

            if (violations.Count > 0)
            {
                throw new InvalidOperationException(
                    "PublicExportSafetyGuard failed — the following fields must not exist on a public export DTO:" +
                    Environment.NewLine + string.Join(Environment.NewLine, violations));
            }
        }
    }
}
