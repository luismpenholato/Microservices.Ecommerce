#Requires -Version 5.1
<#
.SYNOPSIS
    Sobe o ambiente Docker Compose e valida health, métricas e rotas do Gateway.
.EXAMPLE
    .\scripts\validate-local.ps1
.EXAMPLE
    .\scripts\validate-local.ps1 -SkipCompose
.EXAMPLE
    .\scripts\validate-local.ps1 -RunCheckoutFlow
#>
[CmdletBinding()]
param(
    [switch]$SkipCompose,
    [switch]$RunCheckoutFlow,
    [int]$HealthTimeoutMinutes = 12,
    [int]$CheckoutPollTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ComposeFile = Join-Path $RepoRoot 'docker-compose.yml'

$script:Results = [System.Collections.Generic.List[object]]::new()

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Add-Result([string]$Name, [bool]$Success, [string]$Detail) {
    $script:Results.Add([pscustomobject]@{ Name = $Name; Success = $Success; Detail = $Detail })
    $color = if ($Success) { 'Green' } else { 'Red' }
    $status = if ($Success) { 'OK' } else { 'FAIL' }
    Write-Host "  [$status] $Name — $Detail" -ForegroundColor $color
}

function Test-DockerAvailable {
    try {
        $null = docker version 2>&1
        $null = docker compose version 2>&1
        Add-Result 'Docker CLI' $true 'docker e docker compose disponíveis'
        return $true
    }
    catch {
        Add-Result 'Docker CLI' $false $_.Exception.Message
        return $false
    }
}

function Invoke-ComposeUp {
    Push-Location $RepoRoot
    try {
        Write-Step 'Executando docker compose up -d --build (pode levar vários minutos na primeira vez)'
        docker compose -f $ComposeFile up -d --build
        if ($LASTEXITCODE -ne 0) {
            Add-Result 'docker compose up' $false "exit code $LASTEXITCODE"
            return $false
        }
        Add-Result 'docker compose up' $true 'containers iniciados'
        return $true
    }
    finally {
        Pop-Location
    }
}

function Wait-ComposeServicesHealthy {
    $servicesWithHealth = @(
        'postgres', 'redis', 'rabbitmq',
        'identity-api', 'catalog-api', 'basket-api', 'ordering-api', 'inventory-api',
        'payment-worker', 'notification-worker', 'api-gateway'
    )

    $deadline = (Get-Date).AddMinutes($HealthTimeoutMinutes)
    Write-Step "Aguardando healthchecks (timeout ${HealthTimeoutMinutes} min)"

    while ((Get-Date) -lt $deadline) {
        $pending = @()
        foreach ($svc in $servicesWithHealth) {
            $json = docker compose -f $ComposeFile ps $svc --format json 2>$null
            if ([string]::IsNullOrWhiteSpace($json)) {
                $pending += "$svc (ausente)"
                continue
            }
            $row = $json | ConvertFrom-Json
            if ($row.Health -ne 'healthy') {
                $health = if ($row.Health) { $row.Health } else { $row.State }
                $pending += "$svc ($health)"
            }
        }

        if ($pending.Count -eq 0) {
            Add-Result 'Compose healthchecks' $true 'todos os serviços com health=healthy'
            return $true
        }

        Write-Host "  Aguardando: $($pending -join ', ')" -ForegroundColor DarkYellow
        Start-Sleep -Seconds 8
    }

    Add-Result 'Compose healthchecks' $false "timeout após ${HealthTimeoutMinutes} min — pendentes: $($pending -join '; ')"
    return $false
}

function Get-JsonProperty {
    param($Object, [string[]]$Names)
    foreach ($name in $Names) {
        if ($null -ne $Object.PSObject.Properties[$name]) {
            return $Object.PSObject.Properties[$name].Value
        }
    }
    return $null
}

function Get-DemoAuthContext {
    param([string]$GatewayBase)

    $loginBody = @{
        email    = 'demo@ecommerce.local'
        password = 'Demo123!'
    } | ConvertTo-Json -Compress

    $login = Invoke-RestMethod `
        -Uri "$GatewayBase/identity/auth/login" `
        -Method Post `
        -ContentType 'application/json' `
        -Body $loginBody `
        -TimeoutSec 45

    $token = Get-JsonProperty $login @('accessToken', 'AccessToken')
    $customerId = Get-JsonProperty $login @('customerId', 'CustomerId')
    if (-not $token -or -not $customerId) {
        throw 'Login demo não retornou accessToken/customerId.'
    }

    return @{
        Token      = [string]$token
        CustomerId = [guid]$customerId
        Headers    = @{ Authorization = "Bearer $token" }
    }
}

function Invoke-CheckoutFlow {
    param([string]$GatewayBase = 'http://localhost:5000')

    Write-Step 'Fluxo E2E de checkout (Gateway)'

    try {
        $auth = Get-DemoAuthContext -GatewayBase $GatewayBase
        Add-Result 'E2E identity login' $true "customerId=$($auth.CustomerId)"
    }
    catch {
        Add-Result 'E2E identity login' $false $_.Exception.Message
        return $false
    }

    $maxAttempts = 3
    $attempt = 0
    $completed = $false
    $lastDetail = ''

    while (-not $completed -and $attempt -lt $maxAttempts) {
        $attempt++
        if ($attempt -gt 1) {
            Write-Host "  Nova tentativa ($attempt/$maxAttempts) após estado final inesperado." -ForegroundColor DarkYellow
        }

        try {
            $products = Invoke-RestMethod -Uri "$GatewayBase/catalog/products" -Method Get -TimeoutSec 45
            $product = @($products)[0]
            if ($null -eq $product) {
                Add-Result 'E2E catalog products' $false 'lista de produtos vazia'
                return $false
            }

            $productId = Get-JsonProperty $product @('id', 'Id')
            $productName = Get-JsonProperty $product @('name', 'Name')
            $unitPrice = [decimal](Get-JsonProperty $product @('price', 'Price'))
            Add-Result 'E2E catalog products' $true "produto $productId"

            $inventory = Invoke-RestMethod -Uri "$GatewayBase/inventory/inventory/$productId" -Method Get -TimeoutSec 45
            $available = Get-JsonProperty $inventory @('availableQuantity', 'AvailableQuantity')
            if ($null -eq $available -or [int]$available -lt 1) {
                Add-Result 'E2E inventory' $false "estoque indisponível para $productId"
                return $false
            }
            Add-Result 'E2E inventory' $true "availableQuantity=$available"

            $customerId = $auth.CustomerId
            $addBody = @{
                productId   = $productId.ToString()
                productName = [string]$productName
                unitPrice   = $unitPrice
                quantity    = 1
            } | ConvertTo-Json -Compress

            $null = Invoke-RestMethod `
                -Uri "$GatewayBase/basket/baskets/$customerId/items" `
                -Method Post `
                -Headers $auth.Headers `
                -ContentType 'application/json' `
                -Body $addBody `
                -TimeoutSec 45
            Add-Result 'E2E basket add item' $true "customerId=$customerId"

            $checkout = Invoke-RestMethod `
                -Uri "$GatewayBase/basket/baskets/$customerId/checkout" `
                -Method Post `
                -Headers $auth.Headers `
                -TimeoutSec 60

            $orderId = Get-JsonProperty $checkout @('orderId', 'OrderId')
            if (-not $orderId) {
                Add-Result 'E2E basket checkout' $false 'OrderId ausente na resposta'
                return $false
            }
            Add-Result 'E2E basket checkout' $true "orderId=$orderId"

            $terminalStates = @('Completed', 'Failed', 'Cancelled', 'PaymentRejected')
            $deadline = (Get-Date).AddSeconds($CheckoutPollTimeoutSeconds)
            $finalStatus = $null

            while ((Get-Date) -lt $deadline) {
                $order = Invoke-RestMethod -Uri "$GatewayBase/ordering/orders/$orderId" -Method Get -Headers $auth.Headers -TimeoutSec 45
                $finalStatus = [string](Get-JsonProperty $order @('status', 'Status'))
                if ($finalStatus -in $terminalStates) { break }
                Start-Sleep -Seconds 3
            }

            if ($finalStatus -notin $terminalStates) {
                Add-Result 'E2E order polling' $false "timeout ${CheckoutPollTimeoutSeconds}s — último status: $finalStatus"
                return $false
            }

            Write-Host "  Status final do pedido $orderId : $finalStatus" -ForegroundColor White

            if ($finalStatus -eq 'Completed') {
                Add-Result 'E2E order polling' $true "status=$finalStatus"
                $completed = $true
            }
            else {
                $lastDetail = "status=$finalStatus (esperado Completed)"
                Add-Result "E2E order polling (tentativa $attempt)" $false $lastDetail
            }
        }
        catch {
            Add-Result "E2E checkout (tentativa $attempt)" $false $_.Exception.Message
            return $false
        }
    }

    if (-not $completed) {
        Add-Result 'E2E order completed' $false $lastDetail
        return $false
    }

    # Idempotência HTTP no Ordering (mesma Idempotency-Key → mesmo OrderId)
    Write-Step 'Validando idempotência HTTP (Ordering)'
    try {
        $registerEmail = "validate-local-$([guid]::NewGuid())@ecommerce.local"
        $registerBody = @{ email = $registerEmail; password = 'ValidateLocal123!' } | ConvertTo-Json -Compress
        $registered = Invoke-RestMethod `
            -Uri "$GatewayBase/identity/auth/register" `
            -Method Post `
            -ContentType 'application/json' `
            -Body $registerBody `
            -TimeoutSec 45

        $idempotencyToken = Get-JsonProperty $registered @('accessToken', 'AccessToken')
        $idempotencyKey = "validate-local-$([guid]::NewGuid())"
        $orderBody = @{
            items = @(
                @{
                    productId   = $productId.ToString()
                    productName = [string]$productName
                    quantity    = 1
                    unitPrice   = $unitPrice
                }
            )
        } | ConvertTo-Json -Compress

        $headers = @{
            Authorization   = "Bearer $idempotencyToken"
            'Idempotency-Key' = $idempotencyKey
            'Content-Type'    = 'application/json'
        }

        $first = Invoke-RestMethod `
            -Uri "$GatewayBase/ordering/orders" `
            -Method Post `
            -Headers $headers `
            -Body $orderBody `
            -TimeoutSec 45

        $second = Invoke-RestMethod `
            -Uri "$GatewayBase/ordering/orders" `
            -Method Post `
            -Headers $headers `
            -Body $orderBody `
            -TimeoutSec 45

        $firstId = Get-JsonProperty $first @('id', 'Id')
        $secondId = Get-JsonProperty $second @('id', 'Id')

        if ($firstId -and $secondId -and $firstId.ToString() -eq $secondId.ToString()) {
            Add-Result 'E2E idempotency HTTP' $true "orderId=$firstId (chave $idempotencyKey)"
            return $true
        }

        Add-Result 'E2E idempotency HTTP' $false "OrderIds diferentes: $firstId vs $secondId"
        return $false
    }
    catch {
        Add-Result 'E2E idempotency HTTP' $false $_.Exception.Message
        return $false
    }
}

function Test-HttpEndpoint {
    param(
        [string]$Name,
        [string]$Url,
        [scriptblock]$ValidateContent
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 45
        if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
            Add-Result $Name $false "HTTP $($response.StatusCode) — $Url"
            return
        }

        if ($ValidateContent) {
            $ok = & $ValidateContent $response.Content
            if (-not $ok) {
                Add-Result $Name $false "resposta HTTP $($response.StatusCode) sem conteúdo esperado — $Url"
                return
            }
        }

        Add-Result $Name $true "HTTP $($response.StatusCode) — $Url"
    }
    catch {
        Add-Result $Name $false "$($_.Exception.Message) — $Url"
    }
}

Write-Host 'Microservices.Ecommerce — validação local' -ForegroundColor White
Write-Host "Repositório: $RepoRoot"

if (-not (Test-DockerAvailable)) {
    exit 1
}

if (-not $SkipCompose) {
    if (-not (Invoke-ComposeUp)) { exit 1 }
}
else {
    Write-Step 'SkipCompose: pulando docker compose up'
}

$healthOk = Wait-ComposeServicesHealthy

$endpoints = @(
    @{ Name = 'Gateway /health/ready'; Url = 'http://localhost:5000/health/ready' },
    @{ Name = 'Catalog /health/ready'; Url = 'http://localhost:5001/health/ready' },
    @{ Name = 'Basket /health/ready'; Url = 'http://localhost:5002/health/ready' },
    @{ Name = 'Ordering /health/ready'; Url = 'http://localhost:5003/health/ready' },
    @{ Name = 'Inventory /health/ready'; Url = 'http://localhost:5004/health/ready' },
    @{ Name = 'Payment.Worker /health/ready'; Url = 'http://localhost:5010/health/ready' },
    @{ Name = 'Notification.Worker /health/ready'; Url = 'http://localhost:5011/health/ready' },
    @{ Name = 'Identity /health/ready'; Url = 'http://localhost:5005/health/ready' }
)

Write-Step 'Validando /health/ready (host)'
foreach ($ep in $endpoints) {
    Test-HttpEndpoint -Name $ep.Name -Url $ep.Url
}

$metricsValidate = {
    param($content)
    return ($content -match 'ecommerce_') -or ($content -match '# HELP') -or ($content -match '# TYPE')
}

$metricsEndpoints = @(
    @{ Name = 'Gateway /metrics'; Url = 'http://localhost:5000/metrics' },
    @{ Name = 'Catalog /metrics'; Url = 'http://localhost:5001/metrics' },
    @{ Name = 'Basket /metrics'; Url = 'http://localhost:5002/metrics' },
    @{ Name = 'Ordering /metrics'; Url = 'http://localhost:5003/metrics' },
    @{ Name = 'Inventory /metrics'; Url = 'http://localhost:5004/metrics' },
    @{ Name = 'Payment.Worker /metrics'; Url = 'http://localhost:5010/metrics' },
    @{ Name = 'Notification.Worker /metrics'; Url = 'http://localhost:5011/metrics' }
)

Write-Step 'Validando /metrics (host)'
foreach ($ep in $metricsEndpoints) {
    Test-HttpEndpoint -Name $ep.Name -Url $ep.Url -ValidateContent $metricsValidate
}

$sampleProductId = '11111111-1111-1111-1111-111111111101'

Write-Step 'Validando rotas via ApiGateway'
Test-HttpEndpoint -Name 'Gateway GET /catalog/products' -Url 'http://localhost:5000/catalog/products' -ValidateContent {
    param($content)
    return ($content -match 'Notebook') -or ($content -match $sampleProductId)
}
Test-HttpEndpoint -Name 'Gateway GET /inventory/{productId}' -Url "http://localhost:5000/inventory/inventory/$sampleProductId" -ValidateContent {
    param($content)
    return ($content -match 'availableQuantity') -or ($content -match 'AvailableQuantity') -or ($content -match $sampleProductId)
}

$checkoutFlowOk = $true
if ($RunCheckoutFlow) {
    $checkoutFlowOk = Invoke-CheckoutFlow
}
else {
    Write-Host "`n(Dica: use -RunCheckoutFlow para validar checkout ponta a ponta)" -ForegroundColor DarkGray
}

Write-Step 'Resumo'
$passed = @($script:Results | Where-Object Success)
$failed = @($script:Results | Where-Object { -not $_.Success })

Write-Host "`nTotal: $($script:Results.Count) | OK: $($passed.Count) | Falhas: $($failed.Count)" -ForegroundColor White

if ($failed.Count -gt 0) {
    Write-Host "`nFalhas:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $($_.Name): $($_.Detail)" -ForegroundColor Red }
}

if (-not $healthOk -or $failed.Count -gt 0 -or ($RunCheckoutFlow -and -not $checkoutFlowOk)) {
    Write-Host "`nValidação concluída com FALHAS." -ForegroundColor Red
    exit 1
}

Write-Host "`nValidação concluída com SUCESSO." -ForegroundColor Green
if ($RunCheckoutFlow) {
    Write-Host 'Checkout E2E e idempotência HTTP validados.'
}
else {
    Write-Host 'Próximo passo: -RunCheckoutFlow ou docs/smoke-tests.md'
}
exit 0
