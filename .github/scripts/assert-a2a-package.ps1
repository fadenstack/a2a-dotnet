<#
.SYNOPSIS
Asserts that a packed A2A NuGet package ships every target framework and its dependency groups.
#>
param(
    [Parameter(Mandatory = $true)][string]$PackagePath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $PackagePath))
try {
    $entries = $archive.Entries | ForEach-Object { $_.FullName }
    foreach ($tfm in 'netstandard2.0', 'net8.0', 'net10.0') {
        if ($entries -notcontains "lib/$tfm/A2A.dll") {
            throw "Package is missing lib/$tfm/A2A.dll"
        }
    }

    $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -eq 'A2A.nuspec' }
    $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
    try { $nuspec = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
}
finally {
    $archive.Dispose()
}

$ns = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
$ns.AddNamespace('n', $nuspec.DocumentElement.NamespaceURI)
$expected = @{
    '.NETStandard2.0' = @('Microsoft.Extensions.AI.Abstractions', 'Microsoft.Extensions.Logging.Abstractions', 'System.Linq.AsyncEnumerable', 'System.Net.ServerSentEvents', 'System.Text.Json', 'System.Threading.Channels')
    'net8.0'          = @('Microsoft.Extensions.AI.Abstractions', 'Microsoft.Extensions.Logging.Abstractions', 'System.Linq.AsyncEnumerable', 'System.Net.ServerSentEvents', 'System.Text.Json')
    'net10.0'         = @('Microsoft.Extensions.AI.Abstractions', 'Microsoft.Extensions.Logging.Abstractions')
}

foreach ($tfm in $expected.Keys) {
    $group = $nuspec.SelectSingleNode("//n:dependencies/n:group[@targetFramework='$tfm']", $ns)
    if ($null -eq $group) {
        throw "Package has no dependency group for $tfm"
    }

    $actual = @($group.SelectNodes('n:dependency', $ns) | ForEach-Object { $_.GetAttribute('id') }) | Sort-Object
    $wanted = $expected[$tfm] | Sort-Object
    if (Compare-Object $wanted $actual) {
        throw "Dependency group $tfm differs. Expected: $($wanted -join ', '). Actual: $($actual -join ', ')"
    }
}

Write-Host "Package $PackagePath contains lib/netstandard2.0, lib/net8.0, lib/net10.0 and the expected dependency groups."
