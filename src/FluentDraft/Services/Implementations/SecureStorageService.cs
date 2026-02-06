using System;
using System.Security.Cryptography;
using System.Text;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class SecureStorageService : ISecureStorageService
    {
        // Use DPAPI for encryption. DataProtectedScope.CurrentUser ensures only the user running the app can decrypt it.
        // Optional entropy can be added for extra security, but keeping it simple for now.

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
