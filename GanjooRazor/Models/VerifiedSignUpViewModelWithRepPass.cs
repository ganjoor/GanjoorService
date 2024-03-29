﻿using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Models
{
    /// <summary>
    /// VerifiedSignUpViewModel with PasswordConfirmation field
    /// </summary>
    public class VerifiedSignUpViewModelWithRepPass : VerifiedSignUpViewModel
    {
        /// <summary>
        /// password confirmation
        /// </summary>
        public string PasswordConfirmation { get; set; }
    }
}
