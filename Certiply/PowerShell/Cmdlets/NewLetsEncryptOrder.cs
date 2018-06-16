using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Certiply.PowerShell.Cmdlets
{
    [OutputType(typeof(CertesWrapper.DnsValidationRecord[]))]
    [Cmdlet(VerbsCommon.New, "LetsEncryptOrder")]
    public class NewLetsEncryptOrder : AsyncCmdlet
    {
        [ValidateNotNull]
        [Parameter(Mandatory = true, Position = 0)]
        public CertiplyConfig Configuration { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, Position = 1)]
        public string[] Domains { get; set; }

        bool _IgnoreWildcardWarning;

        [Parameter(Mandatory = false, Position = 2)]
        public SwitchParameter IgnoreWildcardWarning
        {
            get { return _IgnoreWildcardWarning; }
            set { _IgnoreWildcardWarning = value; }
        }

        CertesWrapper _Wrapper;

        protected override async Task BeginProcessingAsync(CancellationToken cancellationToken)
        {
            //setup the wrapper with our account
            if (!string.IsNullOrWhiteSpace(Configuration.LetsEncryptServerUrl))
                _Wrapper = new CertesWrapper(Configuration.CertManager, new Uri(Configuration.LetsEncryptServerUrl), cancellationToken);
            else
                _Wrapper = new CertesWrapper(Configuration.CertManager, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;
            
            await _Wrapper.AuthenticateAsync(Configuration.AccountEmail);
        }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var dnsRecords = await _Wrapper.BeginOrderAsync(Domains, IgnoreWildcardWarning);
            WriteObject(dnsRecords);

            _Wrapper = null;
        }
    }
}
