using System.Management.Automation;

namespace Certiply.PowerShell.Cmdlets
{
    [OutputType(typeof(CertiplyConfig))]
    [Cmdlet(VerbsCommon.New, "CertiplyConfig")]
    public class NewCertiplyConfig : Cmdlet
    {
        [ValidateNotNull]
        [Parameter(Mandatory = true, Position = 0)]
        public ICertManager CertManager { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, Position = 1)]
        public string AccountEmail { get; set; }

        [Parameter(Mandatory = false, Position = 2)]
        public string LetsEncryptServerUrl { get; set; }

        [Parameter(Mandatory = false, Position = 3)]
        public string DistinguishedName { get; set; }

        [Parameter(Mandatory = false, Position = 4)]
        public string DnsValidationRecordName { get; set; }

        [Parameter(Mandatory = false, Position = 5)]
        public int DnsCheckRetryLimit { get; set; }

        [Parameter(Mandatory = false, Position = 6)]
        public int DnsCheckRetryInterval { get; set; }

        [Parameter(Mandatory = false, Position = 7)]
        public int ValidationRetryLimit { get; set; }

        [Parameter(Mandatory = false, Position = 8)]
        public int ValidationRetryInterval { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(new CertiplyConfig()
            {
                CertManager = this.CertManager,
                AccountEmail = this.AccountEmail,
                LetsEncryptServerUrl = this.LetsEncryptServerUrl,
                DistinguishedName = this.DistinguishedName,
                DnsValidationRecordName = this.DnsValidationRecordName,
                DnsCheckRetryLimit = this.DnsCheckRetryLimit,
                DnsCheckRetryInterval = this.DnsCheckRetryInterval,
                ValidationRetryLimit = this.ValidationRetryLimit,
                ValidationRetryInterval = this.ValidationRetryInterval
            });
        }
    }
}
