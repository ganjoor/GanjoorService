using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace RSecurityBackend.Utilities
{
    /// <summary>
    /// Audit.NET Environment Skipping
    /// https://stackoverflow.com/questions/59627835/dont-include-environment-in-serialised-auditevents
    /// </summary>    
    public class AuditNetEnvironmentSkippingContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// instance
        /// </summary>
        public static readonly AuditNetEnvironmentSkippingContractResolver Instance = new AuditNetEnvironmentSkippingContractResolver();

        /// <summary>
        /// create property
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = instance => member.Name != "Environment";
            return property;
        }
    }
}
