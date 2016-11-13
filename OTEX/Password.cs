using System;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// Encapsulation of an encrypted password.
    /// </summary>
    [Serializable]
    public sealed class Password
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Key for encrypting and decrypting passwords.
        /// </summary>
        private static readonly Guid EncryptionKey = new Guid("38751C0B-2842-43F4-8059-6E3AD3FAAD55");

        /// <summary>
        /// The encrypted version of the original input password.
        /// </summary>
        private string EncyptedPassword;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a password.
        /// </summary>
        /// <param name="password">The password value in plain text, between 6 and 32 characters long. Whitespace at the ends is trimmed.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public Password(string password)
        {
            if ((password = (password ?? "").Trim()).Length == 0)
                throw new ArgumentException("password cannot be blank", "password");
            if (password.Length < 6 || password.Length > 32)
                throw new ArgumentOutOfRangeException("password", "password must be between 6 and 32 characters");
            if (password.IndexOfAny(new char[] { '\r', '\n', '\t', '\f', '\a', '\b', '\v' }) != -1)
                throw new ArgumentOutOfRangeException("password", "password contains reserved characters");
            EncyptedPassword = password.Encrypt(EncryptionKey.ToString());
        }

        /////////////////////////////////////////////////////////////////////
        // MATCHING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check if this password matches another password.
        /// </summary>
        /// <param name="otherPassword">The other password object to match against.</param>
        /// <exception cref="ArgumentNullException" />
        internal bool Matches(Password otherPassword)
        {
            if (otherPassword == null)
                throw new ArgumentNullException("otherPassword");
            return otherPassword.EncyptedPassword.Equals(EncyptedPassword);
        }
    }
}
