using System;
using System.Security.Cryptography;
using System.Text;

namespace Ara.Core
{
    public sealed class SupervisorAuth
    {
        private readonly string _pinHash;
        private int _failedAttempts;
        private DateTime _lockoutUntilUtc;

        public SupervisorAuth()
            : this(AppConfig.SupervisorPinHash)
        {
        }

        public SupervisorAuth(string pinHash)
        {
            _pinHash = (pinHash ?? string.Empty).Trim();
        }

        public bool IsConfigured
        {
            get { return _pinHash.Length > 0; }
        }

        public bool IsLockedOut
        {
            get { return DateTime.UtcNow < _lockoutUntilUtc; }
        }

        public int RemainingLockoutSeconds
        {
            get
            {
                var remaining = _lockoutUntilUtc - DateTime.UtcNow;
                return remaining.TotalSeconds > 0 ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
            }
        }

        public bool Verify(string pin)
        {
            if (!IsConfigured || IsLockedOut)
            {
                return false;
            }

            var candidate = HashPin(pin ?? string.Empty);
            if (string.Equals(candidate, _pinHash, StringComparison.OrdinalIgnoreCase))
            {
                _failedAttempts = 0;
                _lockoutUntilUtc = DateTime.MinValue;
                return true;
            }

            _failedAttempts++;
            if (_failedAttempts >= 3)
            {
                _failedAttempts = 0;
                _lockoutUntilUtc = DateTime.UtcNow.AddSeconds(30);
            }

            return false;
        }

        public static string HashPin(string pin)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pin ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                {
                    builder.Append(value.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
