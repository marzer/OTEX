using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// <param name="plainTextPassword">The password value in plain text.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public Password(string plainTextPassword)
        {
            if (plainTextPassword == null)
                throw new ArgumentNullException("plainTextPassword cannot be null");
            if (plainTextPassword.Length == 0)
                throw new ArgumentException("plainTextPassword cannot be blank");
            EncyptedPassword = plainTextPassword.Encrypt(EncryptionKey.ToString());
        }

        /////////////////////////////////////////////////////////////////////
        // MATCHING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check if this password matches another password.
        /// </summary>
        /// <param name="otherPassword">The other password object to match against.</param>
        /// <exception cref="ArgumentNullException" />
        public bool Matches(Password otherPassword)
        {
            if (otherPassword == null)
                throw new ArgumentNullException("otherPassword cannot be null");
            return otherPassword.EncyptedPassword.Equals(EncyptedPassword);
        }
    }
}
