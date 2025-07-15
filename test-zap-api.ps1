# Test OWASP ZAP API connectivity
$apiKey = ""
$zapUrl = "http://localhost:8080"

Write-Host "Testing ZAP API connectivity..." -ForegroundColor Green

# Test 1: Basic connectivity (without API key)
try {
    $response = Invoke-WebRequest -Uri "$zapUrl/JSON/core/view/version/" -Method GET -UseBasicParsing -TimeoutSec 10
    Write-Host "✓ Basic connectivity (no API key): OK" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Basic connectivity (no API key): FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: With API key
try {
    $response = Invoke-WebRequest -Uri "$zapUrl/JSON/core/view/version/?apikey=$apiKey" -Method GET -UseBasicParsing -TimeoutSec 10
    Write-Host "✓ API key authentication: OK" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ API key authentication: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Check UI accessibility (should work)
try {
    $response = Invoke-WebRequest -Uri "$zapUrl/UI/core/" -Method GET -UseBasicParsing -TimeoutSec 10
    Write-Host "✓ UI accessibility: OK" -ForegroundColor Green
    Write-Host "Response length: $($response.Content.Length) characters" -ForegroundColor Cyan
} catch {
    Write-Host "✗ UI accessibility: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Check if API is disabled
try {
    $response = Invoke-WebRequest -Uri "$zapUrl/JSON/core/view/status/" -Method GET -UseBasicParsing -TimeoutSec 10
    Write-Host "✓ API status check (no key): OK" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ API status check (no key): FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Test session creation
try {
    $sessionName = "test-session-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $response = Invoke-WebRequest -Uri "$zapUrl/JSON/core/action/newSession/?name=$sessionName&apikey=$apiKey" -Method GET -UseBasicParsing -TimeoutSec 10
    Write-Host "✓ Session creation: OK" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Session creation: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nZAP API Test Complete" -ForegroundColor Green
Write-Host "If tests are failing, check:" -ForegroundColor Yellow
Write-Host "1. ZAP is running with API enabled" -ForegroundColor Yellow
Write-Host "2. API key matches in ZAP settings" -ForegroundColor Yellow
Write-Host "3. localhost/127.0.0.1 is permitted in API settings" -ForegroundColor Yellow
Write-Host "4. 'Secure Only' is disabled in API settings" -ForegroundColor Yellow