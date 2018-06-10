using System;
using System.IO;

namespace Certiply
{
    /// <summary>
    /// Represents a simple file system backed certificate manager
    /// </summary>
    public class FileSystemCertManager : ICertManager
    {
        public const string ACCOUNTKEYIDENTIFIER = "account/key.pem";
        public const string CERTSIDENTIFIERCOMPONENT = "certs";
        public const string ORDERURIIDENTIFIERCOMPONENT = "orderuri";
        public const string CERTPRIVATEKEYIDENTIFIERCOMPONENT = "key.pem";
        public const string CERTISSUERIDENTIFIERCOMPONENT = "issuer.pem";
        public const string CERTIFICATEIDENTIFIERCOMPONENT = "cert.pem";

        string _OrderHome = string.Empty;

        /// <summary>
        /// The full path where the account, order, and certificate are stored
        /// </summary>
        public string StorageRootPath { get; }

        /// <summary>
        /// The common name for the certificate
        /// </summary>
        public string CN { get; private set; }

        /// <summary>
        /// The key for the Let's Encrypt account
        /// </summary>
        public string AccountKey
        {
            get { return ReadFile(ACCOUNTKEYIDENTIFIER); }
            set { WriteFile(ACCOUNTKEYIDENTIFIER, value); }
        }

        string OrderUriPath { get { return Path.Combine(_OrderHome, ORDERURIIDENTIFIERCOMPONENT); }}

        /// <summary>
        /// The URI of the Let's Encrypt order so that it can be resumed
        /// </summary>
        public string OrderUri
        {
            get { return ReadFile(OrderUriPath); }
            set { WriteFile(OrderUriPath, value); }
        }

        string CertPrivateKeyPath { get { return Path.Combine(_OrderHome, CERTPRIVATEKEYIDENTIFIERCOMPONENT); } }

        /// <summary>
        /// The private key for the certificate
        /// </summary>
        public string CertPrivateKey
        {
            get { return ReadFile(CertPrivateKeyPath); }
            set { WriteFile(CertPrivateKeyPath, value); }
        }

        string CertIssuerPath { get { return Path.Combine(_OrderHome, CERTISSUERIDENTIFIERCOMPONENT); } }

        /// <summary>
        /// The full chain of issuers for the certificate
        /// </summary>
        public string CertIssuer
        {
            get { return ReadFile(CertIssuerPath); }
            set { WriteFile(CertIssuerPath, value); }
        }

        string CertificatePath { get { return Path.Combine(_OrderHome, CERTIFICATEIDENTIFIERCOMPONENT); } }

        /// <summary>
        /// The certificate for the <see cref="CN"/>
        /// </summary>
        public string Certificate
        {
            get { return ReadFile(CertificatePath); }
            set { WriteFile(CertificatePath, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Certiply.FileSystemCertManager"/> class.
        /// </summary>
        /// <param name="storageRootPath">The root file system path where the cert manager will hold data</param>
        public FileSystemCertManager(string storageRootPath)
        {
            if (string.IsNullOrWhiteSpace(storageRootPath))
                throw new ArgumentNullException(nameof(storageRootPath));
            if (!Directory.Exists(storageRootPath))
                throw new ArgumentException("Path not found", nameof(storageRootPath));
            
            StorageRootPath = storageRootPath;
        }

        /// <summary>
        /// Sets up the cert manager for the given common name
        /// </summary>
        /// <param name="cn">Common name to use</param>
        public string InitForCommonName(string cn)
        {
            CN = cn;
            _OrderHome = Path.Combine(StorageRootPath, CERTSIDENTIFIERCOMPONENT, cn);
            return _OrderHome;
        }

        string ReadFile(string identifier)
        {
            string fullPath = Path.Combine(StorageRootPath, identifier);

            if (!File.Exists(fullPath))
                return string.Empty;

            return File.ReadAllText(fullPath);
        }

        bool WriteFile(string identifier, string contents)
        {
            string fullPath = Path.Combine(StorageRootPath, identifier);

            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            try
            {
                File.WriteAllText(fullPath, contents);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to write file '{identifier}': {ex.Message}");
                throw;
            }
        }
    }
}
