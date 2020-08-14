
namespace RSecurityBackend.Models.Auth.Memory
{
    /// <summary>
    /// SecurableItemOperation Prerequisite
    /// </summary>
    public class SecurableItemOperationPrerequisite
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="secureItemShortName"></param>
        /// <param name="operationShortName"></param>
        public SecurableItemOperationPrerequisite(string secureItemShortName, string operationShortName)
        {
            SecureItemShortName = secureItemShortName;
            OperationShortName = operationShortName;
        }
        /// <summary>
        /// Prerequisite SecureItem ShortName
        /// </summary>
        /// <example>
        /// job
        /// </example>
        public string SecureItemShortName { get; set; }

        /// <summary>
        /// Prerequisite Operation ShortName
        /// </summary>
        /// <example>
        /// view
        /// </example>
        public string OperationShortName { get; set; }
    }
}
