using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace HealthTrack.Data
{
    /// <summary>
    /// Encrypts and decrypts sensitive health text (e.g. measurement notes, consultation notes)
    /// using the ASP.NET Core Data Protection API (AES-256 encryption + HMAC integrity check).
    /// It is used by ApplicationDbContext so values are encrypted automatically before they are
    /// written to SQL Server and decrypted automatically when they are read back.
    /// </summary>
    public static class HealthDataEncryption
    {
        /// <summary>Purpose string: data protected for this purpose cannot be decrypted by any other part of the app.</summary>
        public const string Purpose = "HealthTrack.SensitiveHealthData.v1";

        /// <summary>Encrypts plain text before it is saved to the database.</summary>
        public static string Encrypt(IDataProtector protector, string plainText)
        {
            return protector.Protect(plainText);
        }

        /// <summary>
        /// Decrypts text read from the database. If the value cannot be decrypted
        /// (for example an old record saved before encryption was added), the stored
        /// value is returned as it is, so the page still works instead of crashing.
        /// </summary>
        public static string Decrypt(IDataProtector protector, string storedText)
        {
            try
            {
                return protector.Unprotect(storedText);
            }
            catch (CryptographicException)
            {
                return storedText;
            }
        }
    }
}
