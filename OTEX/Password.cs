using System;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// Encapsulation of an encrypted password.
    /// </summary>
    [Serializable]
    public sealed class Password : IEquatable<Password>
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

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
            EncyptedPassword = password.Encrypt();
        }

        /////////////////////////////////////////////////////////////////////
        // EQUALITY
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check if this password matches another object
        /// </summary>
        /// <param name="other">The other object to match against.</param>
        public override bool Equals(object other)
        {
            return Equals(other as Password);
        }

        /// <summary>
        /// Check if this password matches another password.
        /// </summary>
        /// <param name="other">The other password object to match against.</param>
        public bool Equals(Password other)
        {
            if (other == null)
                return false;
            return this == other;
        }

        /// <summary>
        /// Check if two passwords match.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>True if they are both the same instance, both null, or both represent the same internal
        /// password data. False otherwise.</returns>
        public static bool operator ==(Password a, Password b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (((object)a == null) || ((object)b == null))
                return false;
            return a.EncyptedPassword.Equals(b);
        }

        /// <summary>
        /// Check if two passwords do not match.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>False if they are both the same instance, both null, or both represent the same internal
        /// password data. True otherwise.</returns>
        public static bool operator !=(Password a, Password b)
        {
            return !(a == b);
        }

        /// <summary>
        /// GetHashCode() override. Throws a <see cref="NotImplementedException"/> (being able to hash a password defeats the
        /// purpose of encapsulating it).
        /// </summary>
        /// <returns>Nothing. Always throws a <see cref="NotImplementedException"/>.</returns>
        public override int GetHashCode()
        {
            throw new NotImplementedException("Passwords do not implement GetHashCode() (that would defeat the purpose!)");
        }
    }
}
