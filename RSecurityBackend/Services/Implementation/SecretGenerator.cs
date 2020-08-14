using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Secret Generator
    /// </summary>
    public class SecretGenerator : ISecretGenerator
    {
        /// <summary>
        /// Generates a secret
        /// </summary>
        /// <returns></returns>
        public string Generate()
        {
            Random random = new Random(DateTime.Now.Millisecond);

            //a 32 letter string
            string randomString = "";
            for (int i = 0; i < 32; i++)
            {
                randomString += (random.Next(1, 3) == 1 ? random.Next('0', '9') : random.Next(2, 3) == 2 ? random.Next('A', 'Z') : random.Next('a', 'z'));
            }

            // generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: randomString,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            hashed = hashed.Replace(" ", "").Replace("+", "");
            return hashed;
            
        }
    }
}
