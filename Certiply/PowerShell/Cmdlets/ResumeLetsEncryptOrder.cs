using System;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Certiply.PowerShell.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Resume, "LetsEncryptOrder")]
    public class ResumeLetsEncryptOrder : AsyncCmdlet
    {
        [ValidateNotNull]
        [Parameter(Mandatory = true, Position = 0)]
        public CertiplyConfig Configuration { get; set; }

        CertesWrapper _Wrapper;

        protected override async Task BeginProcessingAsync()
        {
            //setup the wrapper with our account
            if (!string.IsNullOrWhiteSpace(Configuration.LetsEncryptServerUrl))
                _Wrapper = new CertesWrapper(Configuration.CertManager, new Uri(Configuration.LetsEncryptServerUrl));
            else
                _Wrapper = new CertesWrapper(Configuration.CertManager);

            if (!string.IsNullOrWhiteSpace(Configuration.DistinguishedName))
                _Wrapper.CertDistinguishedName = Configuration.DistinguishedName;
            if (Configuration.DnsCheckRetryLimit > 0)
                _Wrapper.DnsCheckRetryLimit = Configuration.DnsCheckRetryLimit;
            if (Configuration.DnsCheckRetryInterval > 0)
                _Wrapper.DnsCheckRetryInterval = Configuration.DnsCheckRetryInterval;
            if (Configuration.ValidationRetryLimit > 0)
                _Wrapper.ValidationRetryLimit = Configuration.ValidationRetryLimit;
            if (Configuration.ValidationRetryInterval > 0)
                _Wrapper.ValidationRetryInterval = Configuration.ValidationRetryInterval;

            await _Wrapper.AuthenticateAsync(Configuration.AccountEmail);
        }

        protected override async Task ProcessRecordAsync()
        {
            await _Wrapper.ResumeOrderAsync();

            _Wrapper = null;
        }
    }
}
