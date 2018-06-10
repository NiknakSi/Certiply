namespace Certiply.PowerShell
{
    /// <summary>
    /// Represents a simple set of configuration variables for <see cref="CertesWrapper"/>
    /// </summary>
    public class CertiplyConfig
    {
        /// <summary>
        /// Gets the cert manager which is handling the account and certificates
        /// </summary>
        public ICertManager CertManager { get; set; }

        /// <summary>
        /// The email address for the Let's Encrypt account
        /// </summary>
        /// <value>The account email.</value>
        public string AccountEmail { get; set; }

        /// <summary>
        /// This is not currently used by Let's Encrypt
        /// </summary>
        public string LetsEncryptServerUrl { get; set; }

        /// <summary>
        /// This is not currently used by Let's Encrypt
        /// </summary>
        public string DistinguishedName { get; set; }

        /// <summary>
        /// Gets or sets the left most part of the DNS validation record
        /// </summary>
        /// <value>Defaults to <c>_acme-challenge.</c></value>
        public string DnsValidationRecordName { get; set; }

        /// <summary>
        /// Gets or sets the DNS check retry limit.
        /// </summary>
        /// <value>Defaults to 100</value>
        public int DnsCheckRetryLimit { get; set; }

        /// <summary>
        /// Gets or sets the DNS check retry interval in seconds.
        /// </summary>
        /// <value>Defaults to 30 seconds</value>
        public int DnsCheckRetryInterval { get; set; }

        /// <summary>
        /// Gets or sets the validation retry limit.
        /// </summary>
        /// <value>Defaults to 100</value>
        public int ValidationRetryLimit { get; set; }

        /// <summary>
        /// Gets or sets the validation retry interval in seconds.
        /// </summary>
        /// <value>Defaults to 30 seconds</value>
        public int ValidationRetryInterval { get; set; }
    }
}
