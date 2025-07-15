# Simple ZAP API Test Script
Write-Host "Testing ZAP API step by step..." -ForegroundColor Yellow

$zapUrl = "http://localhost:8080"
$apiKey = ""

# Test 1: Basic ZAP UI (should work)
Write-Host "`n1. Testing ZAP UI..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri $zapUrl -TimeoutSec 5
    Write-Host "✓ ZAP UI is accessible" -ForegroundColor Green
} catch {
    Write-Host "✗ ZAP UI failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Try JSON API without key
Write-Host "`n2. Testing JSON API (no key)..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$zapUrl/JSON/core/view/version/" -TimeoutSec 5
    Write-Host "✓ JSON API works without key" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "✗ JSON API (no key) failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Test 3: Try with API key if no-key failed
    Write-Host "`n3. Testing JSON API (with key)..." -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod -Uri "$zapUrl/JSON/core/view/version/?apikey=$apiKey" -TimeoutSec 5
        Write-Host "✓ JSON API works with key" -ForegroundColor Green
        Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Gray
    } catch {
        Write-Host "✗ JSON API (with key) failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 4: Check ZAP API settings
Write-Host "`n4. Checking ZAP API status..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$zapUrl/JSON/core/view/status/" -TimeoutSec 5
    Write-Host "✓ ZAP status API works" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "✗ ZAP status API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n--- Summary ---" -ForegroundColor Yellow
Write-Host "If JSON API tests failed, check these ZAP settings:" -ForegroundColor White
Write-Host "1. Tools > Options > API > Enable API (checked)" -ForegroundColor White
Write-Host "2. Tools > Options > API > Secure Only (unchecked)" -ForegroundColor White
Write-Host "3. Tools > Options > API > API Key = $apiKey" -ForegroundColor White
Write-Host "4. Tools > Options > API > Addresses: 127.0.0.1, localhost" -ForegroundColor White