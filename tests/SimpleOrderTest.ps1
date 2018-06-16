Import-Module '../Certiply/bin/Debug/netstandard2.0/publish/Certiply.dll'

$certManager = New-FileSystemCertManager -StorageRoot /Users/simonh/Desktop/Certiply -CreatePath
$config = New-CertiplyConfig -CertManager $certManager -AccountEmail 'certiply@niknak.org' -LetsEncryptServerUrl 'https://acme-staging-v02.api.letsencrypt.org/directory'
$validationRecords = New-LetsEncryptOrder -Configuration $config -Domains @('test5.niknak.org','*.test5.niknak.org')

$validationRecords | ForEach-Object {
    Write-Host "Configure DNS record $($_.Domain) with values:"

    $validationRecords.Values | ForEach-Object {
        Write-Host "    $($_)"
    }
}

if ($validationRecords -ne $null) {
    Resume-LetsEncryptOrder -Configuration $config
}

Remove-Module Certiply