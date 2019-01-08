using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;
using Polly;

namespace Certiply
{
    /// <summary>
    /// Represents a wrapper for the Certes library to simplify requesting Let's Encrypt certificates using DNS validation
    /// </summary>
    /// <remarks>Certes can be found on GitHub here: https://github.com/fszlin/certes</remarks>
    public class CertesWrapper
    {
        readonly CancellationTokenSource _CancellationSource = new CancellationTokenSource();
        CancellationToken _CancellationToken;

        ICertManager _CertManager;
        Uri _LetsEncryptServer;
        AcmeContext _Acme;
        string _AccountEmail, _Error;
        IAccountContext _Account;
        IOrderContext _OrderContext;
        Order _Order;

        /// <summary>
        /// Gets the cert manager which is handling the account and certificates
        /// </summary>
        public ICertManager CertManager { get { return _CertManager; } }

        /// <summary>
        /// The email address for the Let's Encrypt account
        /// </summary>
        /// <value>The account email.</value>
        public string AccountEmail { get { return _AccountEmail; }}

        /// <summary>
        /// This is not currently used by Let's Encrypt
        /// </summary>
        public string CertDistinguishedName { get; set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="Certiply.CertesWrapper"/> class.
        /// </summary>
        /// <param name="certManager">An <see cref="Certiply.ICertManager"/> instance to manage the account, order, and certificate data</param>
        public CertesWrapper(ICertManager certManager)
        {
            if (certManager == null)
                throw new ArgumentNullException(nameof(certManager));
            
            Init(certManager, WellKnownServers.LetsEncryptV2, _CancellationSource.Token);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Certiply.CertesWrapper"/> class that supports a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="certManager">An <see cref="Certiply.ICertManager"/> instance to manage the account, order, and certificate data</param>
        /// <param name="cancellationToken">A token that can be used to cancel the current operation</param>
        public CertesWrapper(ICertManager certManager, CancellationToken cancellationToken)
        {
            if (certManager == null)
                throw new ArgumentNullException(nameof(certManager));

            Init(certManager, WellKnownServers.LetsEncryptV2, cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Certiply.CertesWrapper"/> class.
        /// </summary>
        /// <param name="certManager">An <see cref="Certiply.ICertManager"/> instance to manage the account, order, and certificate data</param>
        /// <param name="letsEncryptServer">The Let's Encrypt server URI. Defaults to production V2.</param>
        public CertesWrapper(ICertManager certManager, Uri letsEncryptServer, CancellationToken cancellationToken)
        {
            if (certManager == null)
                throw new ArgumentNullException(nameof(certManager));
            if (letsEncryptServer == null || string.IsNullOrWhiteSpace(letsEncryptServer.ToString()))
                throw new ArgumentNullException(nameof(letsEncryptServer));

            Init(certManager, letsEncryptServer, cancellationToken);
        }

        void Init(ICertManager certManager, Uri letsEncryptServer, CancellationToken cancellationToken)
        {
            _CertManager = certManager;
            _LetsEncryptServer = letsEncryptServer;
            _CancellationToken = cancellationToken;

            //default configuration values
            CertDistinguishedName = "C=CA, ST=State, L=City, O=Dept";
            DnsValidationRecordName = "_acme-challenge.";
            DnsCheckRetryLimit = 100;
            DnsCheckRetryInterval = 30;
            ValidationRetryLimit = 100;
            ValidationRetryInterval = 30;
        }

        /// <summary>
        /// Authenticates with Let's Encrypt, either using an existing key from the <see cref="CertManager"/> 
        /// or by creating a new account using the provided email address.
        /// </summary>
        /// <param name="accountEmail">Account email address.</param>
        public async Task AuthenticateAsync(string accountEmail = null)
        {
            string accountPemKey = _CertManager.AccountKey;

            //load an existing account from its key if possible
            if (!string.IsNullOrWhiteSpace(accountPemKey))
            {
                Console.WriteLine("Loading account");
                IKey accountKey = KeyFactory.FromPem(accountPemKey);

                if (_CancellationToken.IsCancellationRequested)
                    return;
                
                _Acme = new AcmeContext(_LetsEncryptServer, accountKey);
                _Account = await _Acme.Account();
            }

            //otherwise create a new account and store the key
            if (_Account == null && !string.IsNullOrWhiteSpace(accountEmail))
            {
                if (_CancellationToken.IsCancellationRequested)
                    return;

                Console.WriteLine("Creating new account");
                _Acme = new AcmeContext(_LetsEncryptServer);
                _Account = await _Acme.NewAccount(accountEmail, true);

                if (_CancellationToken.IsCancellationRequested)
                    return;

                Console.WriteLine("Saving account key");
                _CertManager.AccountKey = _Acme.AccountKey.ToPem();
            }

            _AccountEmail = accountEmail;
        }

        /// <summary>
        /// A one shot command that does the full order process, and only waits while the user configures DNS records.
        /// The ICertManager object is updated with the certificates when the task completes.
        /// </summary>
        /// <param name="domains">
        /// A string array of domain(s) for the certificate. The first is used as the common name and should not 
        /// contain an asterisk (*). All subsequent domains will be included in the subject alternative name (SAN) field.
        /// </param>
        /// <param name="force">Set to <see langword="true"/> in order to skip exceptions about wildcards without the base domain name</param>
        public async Task OrderAsync(string[] domains, bool force = false)
        {
            if (domains == null || !domains.Any())
                throw new ArgumentException("No domains specified", nameof(domains));
            if (domains.First().StartsWith("*", StringComparison.InvariantCultureIgnoreCase) && !force)
                throw new ArgumentException("Do not place an order for a wildcard certificate without using the base domain as the first in the array, e.g. { 'exmaple.com', '*.example.com' }", nameof(domains));
            
            //start by making sure the order is created
            await BeginOrderAsync(domains, force);

            if (_CancellationToken.IsCancellationRequested)
                return;

            //complete the order process
            await ResumeOrderAsync();
        }

        /// <summary>
        /// Starts the order process by warming up the <see cref="CertManager"/>, loading or creating an order, 
        /// and retrieving the DNS validation details.
        /// </summary>
        /// <returns>An array of <see cref="DnsValidationRecord"/> objects describing the DNS records and values required for validation</returns>
        /// <param name="domains">
        /// A string array of domain(s) for the certificate. The first is used as the common name and should not 
        /// contain an asterisk (*). All subsequent domains will be included in the subject alternative name (SAN) field.
        /// </param>
        /// <param name="force">Set to <see langword="true"/> in order to skip exceptions about wildcards without the base domain name</param>
        public async Task<DnsValidationRecord[]> BeginOrderAsync(string[] domains, bool force = false) 
        {
            if (domains == null || !domains.Any())
                throw new ArgumentException("No domains specified", nameof(domains));
            if (domains.First().StartsWith("*", StringComparison.InvariantCultureIgnoreCase) && !force)
                throw new ArgumentException("Do not place an order for a wildcard certificate without using the base domain as the first in the array, e.g. { 'exmaple.com', '*.example.com' }", nameof(domains));

            Console.WriteLine("Ordering certificate for:");
            foreach (string domain in domains)
                Console.WriteLine("  " + domain);

            if (_CancellationToken.IsCancellationRequested)
                return null;

            //make sure the cert manager is ready to handle the order
            _CertManager.InitForCommonName(domains.First());

            //setup the order
            await LoadOrCreateOrderAsync(domains);

            if (_OrderContext == null)
            {
                _Error = "Unable to create order context";
                Console.WriteLine(_Error);
                throw new Exception(_Error);
            }

            List<DnsValidationRecord> validationRecords = new List<DnsValidationRecord>();

            //return a simplified list of DNS records that need values adding
            foreach (var auth in await _OrderContext.Authorizations())
            {
                if (_CancellationToken.IsCancellationRequested)
                    return null;

                var authResource = await auth.Resource();

                if (authResource.Status != AuthorizationStatus.Valid)
                {
                    string cn = authResource.Identifier.Value;
                    string validationDomain = DnsValidationRecordName + cn;

                    if (_CancellationToken.IsCancellationRequested)
                        return null;

                    var dnsChallenge = await auth.Dns();
                    var dnsTxt = _Acme.AccountKey.DnsTxt(dnsChallenge.Token);

                    var validationRecord = validationRecords.FirstOrDefault(vr => vr.Domain == validationDomain);
                    if (validationRecord != null)
                    {
                        List<string> values = new List<string>(validationRecord.Values);
                        values.Add(dnsTxt);
                        validationRecords.First(vr => vr.Domain == validationDomain).Values = values.ToArray();
                    }
                    else
                        validationRecords.Add(new DnsValidationRecord() { Domain = validationDomain, Values = new string[] { dnsTxt } });
                }
            }

            Console.WriteLine($"Retrieved {validationRecords.Count()} required DNS validation record(s)");
            return validationRecords.ToArray();
        }

        /// <summary>
        /// Resumes and completes an order. DNS validation records must be in place.
        /// </summary>
        public async Task ResumeOrderAsync()
        {
            string orderUri = _CertManager.OrderUri;
            if (string.IsNullOrWhiteSpace(orderUri))
                throw new Exception("Invalid order URI. Check your ICertManager instance has been initialised properly.");

            if (!await LoadOrderAsync(orderUri))
                throw new Exception("Order not found, please begin a new order");

            if (_CancellationToken.IsCancellationRequested)
                return;

            //process each of the authorizations
            foreach (var auth in await _OrderContext.Authorizations())
            {
                if (_CancellationToken.IsCancellationRequested)
                    return;

                if (await ProcessAuthorizationAsync(auth) != AuthorizationStatus.Valid)
                {
                    _Error = "Unable to authorize domain";
                    Console.WriteLine(_Error);
                    throw new Exception(_Error);
                }
            }

            if (_CancellationToken.IsCancellationRequested)
                return;

            //update our order - new orders should be status 'ready' at this point
            _Order = await _OrderContext.Resource();

            if (_CancellationToken.IsCancellationRequested)
                return;

            //see https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.6

            //Order objects are created in the "pending" state. Once all of the
            //authorizations listed in the order object are in the "valid" state,
            //the order transitions to the "ready" state. The order moves to the
            //"processing" state after the client submits a request to the order's
            //"finalize" URL and the CA begins the issuance process for the
            //certificate. Once the certificate is issued, the order enters the
            //"valid" state. If an error occurs at any of these stages, the order
            //moves to the "invalid" state. The order also moves to the "invalid"
            //state if it expires, or one of its authorizations enters a final
            //state other than "valid" ("expired", "revoked", "deactivated").

            if (_Order.Status == OrderStatus.Ready || _Order.Status == OrderStatus.Pending)
                _Order = await FinalizeOrderAsync();

            if (_Order.Status != OrderStatus.Valid)
            {
                _Error = $"Unexpected order status '{_Order.Status}'";
                Console.WriteLine(_Error);
                throw new Exception(_Error);
            }

            await DownloadCertificateAsync();

            Console.WriteLine("Order complete");
        }

        async Task LoadOrCreateOrderAsync(string[] domains)
        {
            string orderUri = _CertManager.OrderUri;

            if (!string.IsNullOrWhiteSpace(orderUri))
                await LoadOrderAsync(orderUri);

            if (_CancellationToken.IsCancellationRequested)
                return;

            if (_OrderContext == null)
            {
                Console.WriteLine("Creating new order");
                _OrderContext = await _Acme.NewOrder(domains);
                _CertManager.OrderUri = _OrderContext.Location.ToString();
            }
        }

        async Task<bool> LoadOrderAsync(string orderUri)
        {
            if (string.IsNullOrWhiteSpace(orderUri))
                throw new ArgumentNullException(nameof(orderUri));

            Console.WriteLine("Loading order");
            _OrderContext = _Acme.Order(new Uri(orderUri));

            if (_OrderContext != null)
                _Order = await _OrderContext.Resource();

            //if the order has or is about to expire, create a new one
            if (_Order.Expires <= DateTime.UtcNow)
            {
                Console.WriteLine("Previous order has expired");
                _Order = null;
                _OrderContext = null;

                return false;
            }

            return true;
        }

        async Task<AuthorizationStatus> ProcessAuthorizationAsync(IAuthorizationContext auth)
        {
            var authResource = await auth.Resource();
            string cn = authResource.Identifier.Value;
            string validationDomain = DnsValidationRecordName + cn;

            if (authResource.Status == AuthorizationStatus.Valid)
            {
                Console.WriteLine($"Authorization already passed for {(authResource.Wildcard.HasValue && authResource.Wildcard.Value ? "*." : "")}{cn}");
                return authResource.Status.Value;
            }
            else
            {
                if (_CancellationToken.IsCancellationRequested)
                    return AuthorizationStatus.Pending;

                Console.WriteLine($"Authorizing {(authResource.Wildcard.HasValue && authResource.Wildcard.Value ? "*." : "")}{cn}");

                //always use dns challenge
                var dnsChallenge = await auth.Dns();
                var dnsTxt = _Acme.AccountKey.DnsTxt(dnsChallenge.Token);

                if (_CancellationToken.IsCancellationRequested)
                    return AuthorizationStatus.Pending;

                bool dnsResult = await Utils.CheckDnsTxtAsync(validationDomain, dnsTxt, _CancellationToken, DnsCheckRetryLimit, DnsCheckRetryInterval);

                //quit if still no dns record after waiting
                if (!dnsResult)
                {
                    Console.WriteLine("DNS validation failed");
                    return AuthorizationStatus.Invalid;
                }

                try
                {
                    //keep retrying the validation until it passes or we exhaust the retries
                    Challenge challenge = await Policy
                        .HandleResult<Challenge>(c => c.Status == ChallengeStatus.Pending || c.Status == ChallengeStatus.Processing)
                        .WaitAndRetryAsync(
                            ValidationRetryLimit,
                            retryAttempt => TimeSpan.FromSeconds(ValidationRetryInterval),
                            (delegateResult, timeSpan, context) =>
                            {
                                Console.WriteLine($"Challenge validation returned status {delegateResult.Result.Status}, retrying in {ValidationRetryInterval}s");
                            })
                        .ExecuteAsync(async () =>
                        {
                            if (_CancellationToken.IsCancellationRequested)
                                return null;

                            Console.WriteLine($"Validating challenge...");
                            return await dnsChallenge.Validate();
                        });

                    if (challenge == null || challenge.Status != ChallengeStatus.Valid)
                    {
                        Console.WriteLine($"ACME validation failed for {cn}");
                        return AuthorizationStatus.Invalid;
                    }

                    Console.WriteLine($"{cn} ok");
                    return AuthorizationStatus.Valid;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return AuthorizationStatus.Invalid;
        }

        async Task<Order> FinalizeOrderAsync()
        {
            Console.WriteLine("Constructing private key and CSR");

            IEnumerable<string> domains = _Order.Identifiers.Where(i => i.Type == IdentifierType.Dns).Select(i => i.Value);

            //when ordering wildcard certs the cn shouldn't be the wildcard but the ordering of the identifiers
            //often does not match the order of domains in the original order setup
            string cn = domains.First().StartsWith("*", StringComparison.InvariantCultureIgnoreCase) && domains.Count() > 1
                               ? domains.Skip(1).Take(1).First() : domains.First();
            var csrBuilder = new CertificationRequestBuilder();

            csrBuilder.AddName($"{CertDistinguishedName}, CN={cn}");

            //setup the san if necessary
            if (domains.Count() > 1)
                csrBuilder.SubjectAlternativeNames = domains.Where(d => d != cn).ToList();

            byte[] csr = csrBuilder.Generate();
            var privateKey = csrBuilder.Key;
            _CertManager.CertPrivateKey = privateKey.ToPem();

            if (_CancellationToken.IsCancellationRequested)
                return null;

            Console.WriteLine("Finalizing order");
            try
            {
                return await _OrderContext.Finalize(csr);
            }
            catch (AcmeRequestException ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.Error != null)
                    Console.WriteLine(ex.Error.Detail);
            }

            if (_CancellationToken.IsCancellationRequested)
                return null;

            return await _OrderContext.Resource();
        }

        async Task DownloadCertificateAsync()
        {
            //TODO: Add support for other certificate formats?

            Console.WriteLine("Downloading certificate");
            try
            {
                var certChain = await _OrderContext.Download();

                _CertManager.Certificate = certChain.Certificate.ToPem();

                StringBuilder chainBuilder = new StringBuilder();
                foreach (var issuer in certChain.Issuers)
                    chainBuilder.AppendLine(issuer.ToPem());

                _CertManager.CertIssuer = chainBuilder.ToString();

                //TODO: Output the expiration date of the certificate
                Console.WriteLine($"Certificate stored ok. This order expires at {_Order.Expires}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }


        /// <summary>
        /// Represents a DNS validation record
        /// </summary>
        public class DnsValidationRecord
        {
            /// <summary>
            /// The FQDN of the record required for validation
            /// </summary>
            /// <value>The domain.</value>
            public string Domain { get; set; }

            /// <summary>
            /// An array of string values that the TXT record at <see cref="Domain"/> must contain
            /// </summary>
            /// <value>The values.</value>
            public string[] Values { get; set; }
        }
    }
}
