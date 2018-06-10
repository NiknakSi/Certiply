namespace Certiply
{
    /// <summary>
    /// Describes an API for managing certificates and Let's Encrypt orders
    /// </summary>
    public interface ICertManager
    {
        /// <summary>
        /// The key for the Let's Encrypt account
        /// </summary>
        string AccountKey { get; set; }

        /// <summary>
        /// Sets up the cert manager for the given common name
        /// </summary>
        string CN { get; }

        /// <summary>
        /// The URI of the Let's Encrypt order so that it can be resumed
        /// </summary>
        string OrderUri { get; set; }

        /// <summary>
        /// The private key for the certificate
        /// </summary>
        string CertPrivateKey { get; set; }

        /// <summary>
        /// The full chain of issuers for the certificate
        /// </summary>
        string CertIssuer { get; set; }

        /// <summary>
        /// The certificate for the <see cref="CN"/>
        /// </summary>
        string Certificate { get; set; }


        /// <summary>
        /// Sets up the cert manager for the given common name
        /// </summary>
        /// <param name="cn">Common name to use</param>
        string InitForCommonName(string cn);
    }
}
