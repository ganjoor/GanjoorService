namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// Verify Queue Type
    /// </summary>
    public enum RVerifyQueueType
    {
        /// <summary>
        /// Sign up by email
        /// </summary>
        SignUp = 0,
        /// <summary>
        /// Forgot Password by email
        /// </summary>
        ForgotPassword = 1,
        /// <summary>
        /// delete user by himself/hersef
        /// </summary>
        UserSelfDelete = 2,
        /// <summary>
        /// kick out user
        /// </summary>
        KickOutUser = 3
    }
}
