using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// an encryption / decryption utility for strings (used for hiding spotify api credentials)
    /// source : https://stackoverflow.com/questions/32972126/creating-decrypt-passwords-with-salt-iv
    /// </summary>
    internal static class EncDecUtil
    {
        public static string Encrypt(string text, string pwd)
        {
            byte[] originalBytes = Encoding.UTF8.GetBytes(text);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(pwd);

            // Hash the password with SHA256
            passwordBytes = SHA256.HashData(passwordBytes);

            // Generating salt bytes
            byte[] saltBytes = _GetRandomBytes();

            // Appending salt bytes to original bytes
            byte[] bytesToBeEncrypted = new byte[saltBytes.Length + originalBytes.Length];
            for (int i = 0; i < saltBytes.Length; i++)
            {
                bytesToBeEncrypted[i] = saltBytes[i];
            }
            for (int i = 0; i < originalBytes.Length; i++)
            {
                bytesToBeEncrypted[i + saltBytes.Length] = originalBytes[i];
            }

            byte[] encryptedBytes = _AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string decryptedText, string pwd)
        {
            byte[] bytesToBeDecrypted = Convert.FromBase64String(decryptedText);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(pwd);

            // Hash the password with SHA256
            passwordBytes = SHA256.HashData(passwordBytes);

            byte[] decryptedBytes = _AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            // Getting the size of salt
            int _saltSize = 4;

            // Removing salt bytes, retrieving original bytes
            byte[] originalBytes = new byte[decryptedBytes.Length - _saltSize];
            for (int i = _saltSize; i < decryptedBytes.Length; i++)
            {
                originalBytes[i - _saltSize] = decryptedBytes[i];
            }

            return Encoding.UTF8.GetString(originalBytes);
        }

        private static byte[] _GetRandomBytes()
        {
            int _saltSize = 4;
            byte[] ba = new byte[_saltSize];
            RandomNumberGenerator.Fill(ba);
            return ba;
        }

        private static byte[] _AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;

                // Use the static Pbkdf2 method
                byte[] key = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA1, aes.KeySize / 8 + aes.BlockSize / 8);

                // Extract key and IV from the derived bytes
                aes.Key = key[0..(aes.KeySize / 8)];
                aes.IV = key[(aes.KeySize / 8)..];

                aes.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                    cs.Close();
                }
                encryptedBytes = ms.ToArray();
            }

            return encryptedBytes;
        }

        private static byte[] _AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;

                // Use the static Pbkdf2 method
                byte[] key = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA1, aes.KeySize / 8 + aes.BlockSize / 8);

                // Extract key and IV from the derived bytes
                aes.Key = key[0..(aes.KeySize / 8)];
                aes.IV = key[(aes.KeySize / 8)..];

                aes.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                    cs.Close();
                }
                decryptedBytes = ms.ToArray();
            }

            return decryptedBytes;
        }
    }
}