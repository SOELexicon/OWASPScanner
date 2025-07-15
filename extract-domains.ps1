# PowerShell script to extract unique domains from IIS bindings (port 443 only)
# Input: IIS_Bindings_20250715_105404.json
# Output: test-urls.json

$inputFile = "IIS_Bindings_20250715_105404.json"
$outputFile = "test-urls.json"

# Read and parse the JSON file
$jsonData = Get-Content $inputFile | ConvertFrom-Json

# Extract unique domains from port 443 bindings
$uniqueDomains = @()

foreach ($site in $jsonData) {
    foreach ($binding in $site.Bindings) {
        if ($binding.Port -eq "443" -and $binding.HostHeader -ne "") {
            $domain = $binding.HostHeader
            if ($uniqueDomains -notcontains $domain) {
                $uniqueDomains += $domain
            }
        }
    }
}

# Sort domains alphabetically
$uniqueDomains = $uniqueDomains | Sort-Object

# Convert to JSON array format and save
$uniqueDomains | ConvertTo-Json | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "Extracted $($uniqueDomains.Count) unique domains from port 443 bindings"
Write-Host "Output saved to: $outputFile"
Write-Host "Domains found:"
$uniqueDomains | ForEach-Object { Write-Host "  - $_" }