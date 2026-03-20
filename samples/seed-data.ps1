<#
.SYNOPSIS
    Seeds demo data into the local Elasticsearch instance for ElasticMCP.
.DESCRIPTION
    Creates sample indices with realistic data so you can explore
    ElasticMCP tools interactively via MCP Inspector or Claude Desktop.
#>
param(
    [string]$EsUrl = "http://localhost:9200"
)

$ErrorActionPreference = "Stop"

Write-Host "Waiting for Elasticsearch at $EsUrl..." -ForegroundColor Cyan

for ($i = 0; $i -lt 30; $i++) {
    try {
        $null = Invoke-RestMethod -Uri "$EsUrl/_cluster/health" -TimeoutSec 2
        Write-Host "  Elasticsearch is ready!" -ForegroundColor Green
        break
    } catch {
        Start-Sleep -Seconds 2
    }
    if ($i -eq 29) {
        Write-Host "  Elasticsearch not reachable. Is the container running?" -ForegroundColor Red
        exit 1
    }
}

# ── 1. Server Logs ──────────────────────────────────────────────────
Write-Host ""
Write-Host "Seeding 'server-logs' index..." -ForegroundColor Cyan

$levels = @("info", "warn", "error", "debug")
$services = @("api-gateway", "auth-service", "payment-service", "notification-service", "user-service")
$messages = @{
    "info"  = @("Request processed successfully", "Cache hit for user profile", "Health check passed", "Connection pool refreshed", "Session created for user")
    "warn"  = @("Response time exceeded 500ms", "Cache miss ratio above 30%", "Retry attempt 2 of 3", "Memory usage above 80%", "Rate limit approaching threshold")
    "error" = @("Database connection timeout", "Payment gateway returned 503", "JWT token validation failed", "Null reference in OrderProcessor", "Deadlock detected in transaction")
    "debug" = @("Entering method ProcessOrder", "SQL query executed in 12ms", "Serialized response: 2.3KB", "Thread pool size: 25", "GC collection completed: Gen2")
}

$bulk = ""
$baseDate = (Get-Date).AddDays(-7)

for ($i = 0; $i -lt 100; $i++) {
    $level = $levels[$i % $levels.Count]
    $service = $services[$i % $services.Count]
    $msgList = $messages[$level]
    $msg = $msgList[$i % $msgList.Count]
    $ts = $baseDate.AddMinutes($i * 100).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    $statusCode = if ($level -eq "error") { 500 } elseif ($level -eq "warn") { 429 } else { 200 }
    $responseTime = if ($level -eq "error") { Get-Random -Minimum 1000 -Maximum 5000 } elseif ($level -eq "warn") { Get-Random -Minimum 500 -Maximum 1000 } else { Get-Random -Minimum 10 -Maximum 200 }

    $bulk += "{`"index`":{`"_index`":`"server-logs`"}}`n"
    $bulk += "{`"@timestamp`":`"$ts`",`"level`":`"$level`",`"service`":`"$service`",`"message`":`"$msg`",`"status_code`":$statusCode,`"response_time_ms`":$responseTime,`"host`":`"prod-server-$($i % 5 + 1)`"}`n"
}

Invoke-RestMethod -Uri "$EsUrl/_bulk" -Method Post -Body $bulk -ContentType "application/x-ndjson" | Out-Null
Write-Host "  100 server log entries created" -ForegroundColor Green

# ── 2. Products Catalog ─────────────────────────────────────────────
Write-Host "Seeding 'products' index..." -ForegroundColor Cyan

$categories = @("Electronics", "Books", "Clothing", "Home", "Sports")
$products = @(
    @{name="Wireless Headphones"; price=79.99; category="Electronics"; rating=4.5; in_stock=$true; description="Premium noise-cancelling wireless headphones with 30-hour battery life"},
    @{name="Mechanical Keyboard"; price=149.99; category="Electronics"; rating=4.8; in_stock=$true; description="RGB mechanical keyboard with Cherry MX Blue switches"},
    @{name="4K Monitor"; price=449.99; category="Electronics"; rating=4.3; in_stock=$false; description="27-inch 4K IPS monitor with USB-C connectivity"},
    @{name="Clean Code"; price=34.99; category="Books"; rating=4.7; in_stock=$true; description="A handbook of agile software craftsmanship by Robert C. Martin"},
    @{name="Design Patterns"; price=42.99; category="Books"; rating=4.5; in_stock=$true; description="Elements of reusable object-oriented software by Gang of Four"},
    @{name="The Pragmatic Programmer"; price=39.99; category="Books"; rating=4.9; in_stock=$true; description="Your journey to mastery, 20th anniversary edition"},
    @{name="Running Shoes"; price=129.99; category="Sports"; rating=4.4; in_stock=$true; description="Lightweight trail running shoes with responsive cushioning"},
    @{name="Yoga Mat"; price=29.99; category="Sports"; rating=4.6; in_stock=$true; description="Non-slip exercise mat, 6mm thick, eco-friendly material"},
    @{name="Cotton T-Shirt"; price=19.99; category="Clothing"; rating=4.2; in_stock=$true; description="100% organic cotton crew neck t-shirt, available in 8 colors"},
    @{name="Winter Jacket"; price=189.99; category="Clothing"; rating=4.7; in_stock=$false; description="Waterproof insulated jacket rated to -20C"},
    @{name="Smart Watch"; price=299.99; category="Electronics"; rating=4.1; in_stock=$true; description="Fitness tracker with GPS, heart rate monitor, and 7-day battery"},
    @{name="Standing Desk"; price=599.99; category="Home"; rating=4.6; in_stock=$true; description="Electric height-adjustable standing desk, 60x30 inches"},
    @{name="LED Desk Lamp"; price=44.99; category="Home"; rating=4.4; in_stock=$true; description="Dimmable LED desk lamp with wireless charging base"},
    @{name="Espresso Machine"; price=349.99; category="Home"; rating=4.8; in_stock=$true; description="Semi-automatic espresso machine with built-in grinder"},
    @{name="Dumbbell Set"; price=89.99; category="Sports"; rating=4.3; in_stock=$true; description="Adjustable dumbbell set, 5-50 lbs per hand"}
)

$bulk = ""
foreach ($p in $products) {
    $json = $p | ConvertTo-Json -Compress
    $bulk += "{`"index`":{`"_index`":`"products`"}}`n"
    $bulk += "$json`n"
}

Invoke-RestMethod -Uri "$EsUrl/_bulk" -Method Post -Body $bulk -ContentType "application/x-ndjson" | Out-Null
Write-Host "  $($products.Count) products created" -ForegroundColor Green

# ── 3. Users ────────────────────────────────────────────────────────
Write-Host "Seeding 'users' index..." -ForegroundColor Cyan

$users = @(
    @{name="Alice Johnson"; email="alice@example.com"; role="admin"; department="Engineering"; joined="2023-01-15"; active=$true},
    @{name="Bob Smith"; email="bob@example.com"; role="developer"; department="Engineering"; joined="2023-03-22"; active=$true},
    @{name="Carol Williams"; email="carol@example.com"; role="designer"; department="Product"; joined="2023-06-01"; active=$true},
    @{name="Dave Brown"; email="dave@example.com"; role="developer"; department="Engineering"; joined="2023-09-10"; active=$false},
    @{name="Eve Davis"; email="eve@example.com"; role="manager"; department="Product"; joined="2022-11-05"; active=$true},
    @{name="Frank Miller"; email="frank@example.com"; role="developer"; department="Data"; joined="2024-01-20"; active=$true},
    @{name="Grace Lee"; email="grace@example.com"; role="analyst"; department="Data"; joined="2024-03-15"; active=$true},
    @{name="Hank Wilson"; email="hank@example.com"; role="devops"; department="Infrastructure"; joined="2023-07-28"; active=$true}
)

$bulk = ""
foreach ($u in $users) {
    $json = $u | ConvertTo-Json -Compress
    $bulk += "{`"index`":{`"_index`":`"users`"}}`n"
    $bulk += "$json`n"
}

Invoke-RestMethod -Uri "$EsUrl/_bulk" -Method Post -Body $bulk -ContentType "application/x-ndjson" | Out-Null
Write-Host "  $($users.Count) users created" -ForegroundColor Green

# ── Refresh ─────────────────────────────────────────────────────────
Invoke-RestMethod -Uri "$EsUrl/_refresh" -Method Post | Out-Null

# ── Summary ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Done! Indices available:" -ForegroundColor Cyan
$stats = Invoke-RestMethod -Uri "$EsUrl/_cat/indices?v&s=index"
Write-Host $stats
Write-Host ""
Write-Host "You can now run ElasticMCP and connect via MCP Inspector:" -ForegroundColor Green
Write-Host "  1. dotnet run --project src/ElasticMcp/ElasticMcp.csproj" -ForegroundColor White
Write-Host "  2. npx @anthropic-ai/mcp-inspector" -ForegroundColor White
