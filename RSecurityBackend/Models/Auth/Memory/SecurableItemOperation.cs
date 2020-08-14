namespace RSecurityBackend.Models.Auth.Memory
{
    /// <summary>
    /// operations (rights/permissions) for SecurableItem s
    /// </summary>
    public class SecurableItemOperation
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public SecurableItemOperation() : this("", "", false)
        {

        }
        /// <summary>
        /// one line constructor
        /// </summary>
        /// <param name="shortName"></param>
        /// <param name="description"></param>
        /// <param name="status"></param>
        /// <param name="prerequisites"></param>
        //
        public SecurableItemOperation(string shortName, string description, bool status, SecurableItemOperationPrerequisite[] prerequisites = null )
        {
            ShortName = shortName;
            Description = description;
            Status = status;
            if(prerequisites == null)
            {
                Prerequisites = new SecurableItemOperationPrerequisite[] { };
            }
            else
            {
                Prerequisites = prerequisites;
            }      
            
        }
        /// <summary>
        /// Short Name
        /// </summary>
        /// <example>
        /// view
        /// </example>
        public string ShortName { get; set; }

        /// <summary>
        /// Descripttion
        /// </summary>
        /// <example>
        /// مشاهده
        /// </example>
        public string Description { get; set; }

        /// <summary>
        /// Prerequisites
        /// </summary>
        public SecurableItemOperationPrerequisite[] Prerequisites { get; set; }

     
        /// <summary>
        /// Status (has permission)
        /// </summary>
        public bool Status { get; set; }
    }
}
