using RSecurityBackend.Models.Auth.ViewModels;

namespace RMuseum.Models.Auth.ViewModel
{
    /// <summary>
    /// LoginViewModel with app specfic properties
    /// </summary>
    public class LoggedOnUserModelEx : LoggedOnUserModel
    {
        /// <summary>
        /// keep user browsing history if he or she wants so
        /// </summary>
        public bool KeepHistory { get; set; }
    }
}
