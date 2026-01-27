# Test save configuration API

$config = @{
    userId = "default"
    providers = @(
        @{
            providerType = "pushdeer"
            settings = @{
                name = "Test Provider"
                enabled = "true"
                pushkey = "test123"
            }
        }
    )
    projectRules = @()
    templates = @{}
    lastModified = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
} | ConvertTo-Json -Depth 10

Write-Host "Testing PUT /api/webhook-config/default"
Write-Host "Request body:"
Write-Host $config
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:5000/api/webhook-config/default" `
        -Method Put `
        -Body $config `
        -ContentType "application/json" `
        -UseBasicParsing
    
    Write-Host "Success! Status: $($response.StatusCode)"
    Write-Host "Response:"
    Write-Host $response.Content
} catch {
    Write-Host "Error! Status: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Error message:"
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    Write-Host $reader.ReadToEnd()
}
