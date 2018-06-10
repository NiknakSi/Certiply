using System.Management.Automation;

namespace Certiply.PowerShell.Cmdlets
{
    [OutputType(typeof(FileSystemCertManager))]
    [Cmdlet(VerbsCommon.New, "FileSystemCertManager")]
    public class NewFileSystemCertManager : Cmdlet
    {
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, Position = 0)]
        public string StorageRoot { get; set; }

        bool _CreatePath;

        [Parameter(Mandatory = false, Position = 1)]
        public SwitchParameter CreatePath
        {
            get { return _CreatePath; }
            set { _CreatePath = value; }
        }

        protected override void ProcessRecord()
        {
            if (_CreatePath && !System.IO.Directory.Exists(StorageRoot))
                System.IO.Directory.CreateDirectory(StorageRoot);

            WriteObject(new FileSystemCertManager(StorageRoot));
        }
    }
}
