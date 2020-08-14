using System;
using System.Collections.Generic;
using System.Text;

namespace RSecurityBackend.Services
{
    /// <summary>
    /// Secret Generator
    /// </summary>
    public interface ISecretGenerator
    {
        /// <summary>
        /// Generates a secret
        /// </summary>
        /// <returns></returns>
        string Generate();
    }
}
