// =====================================================================
// FILE:    Data/HealthDataEncryption.cs
// PURPOSE: Two small helpers that encrypt and decrypt sensitive health text
//          (measurement notes and trainer consultation notes) with the ASP.NET
//          Core Data Protection API. ApplicationDbContext calls them on every
//          save and load, so controllers never handle encryption themselves.
// KEY IDEAS: encryption at rest, AES-256 + HMAC, purpose strings, graceful
//            fallback for old unencrypted rows.
// =====================================================================

using System.Security.Cryptography;           // CryptographicException
using Microsoft.AspNetCore.DataProtection;    // IDataProtector

namespace HealthTrack.Data
{
    /// <summary>
    /// Encrypts and decrypts sensitive health text (e.g. measurement notes, consultation notes)
    /// using the ASP.NET Core Data Protection API (AES-256 encryption + HMAC integrity check).
    /// It is used by ApplicationDbContext so values are encrypted automatically before they are
    /// written to SQL Server and decrypted automatically when they are read back.
    /// </summary>
    public static class HealthDataEncryption    // static: just helper methods, no state
    {
        /// <summary>Purpose string: data protected for this purpose cannot be decrypted by any other part of the app.</summary>
        public const string Purpose = "HealthTrack.SensitiveHealthData.v1";

        /// <summary>Encrypts plain text before it is saved to the database.</summary>
        public static string Encrypt(IDataProtector protector, string plainText)
        {
            return protector.Protect(plainText);             // returns unreadable cipher text, e.g. "CfDJ8..."
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
                return protector.Unprotect(storedText);      // cipher text back to the original text
            }
            catch (CryptographicException)                   // not encrypted, or encrypted with other keys
            {
                return storedText;                           // show the stored value instead of crashing
            }
        }
    }
}
