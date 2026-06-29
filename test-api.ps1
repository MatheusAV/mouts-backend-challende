# ============================================================
#  DeveloperStore - Sales API - Test Script
# ============================================================

$BASE = "http://localhost:8080"
$PASS = 0
$FAIL = 0
$ERRORS = @()

function Write-Title($text) {
    Write-Host "`n=== $text ===" -ForegroundColor Cyan
}

function Assert-Status($label, $response, $expectedStatus) {
    if ($response.StatusCode -eq $expectedStatus) {
        Write-Host "  [PASS] $label ($($response.StatusCode))" -ForegroundColor Green
        $script:PASS++
    } else {
        Write-Host "  [FAIL] $label - esperado $expectedStatus, recebido $($response.StatusCode)" -ForegroundColor Red
        if ($response.RawBody) { Write-Host "         Body: $($response.RawBody)" -ForegroundColor DarkRed }
        $script:FAIL++
        $script:ERRORS += $label
    }
}

function Invoke-Api($method, $path, $body = $null, $token = $null) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }

    $jsonBody = $null
    if ($body) { $jsonBody = ($body | ConvertTo-Json -Depth 10) }

    try {
        $params = @{
            Method          = $method
            Uri             = "$BASE$path"
            Headers         = $headers
            UseBasicParsing = $true
        }
        if ($jsonBody) { $params["Body"] = $jsonBody }
        $resp = Invoke-WebRequest @params
        $parsed = $null
        try { $parsed = $resp.Content | ConvertFrom-Json } catch {}
        return [PSCustomObject]@{ StatusCode = [int]$resp.StatusCode; Body = $parsed; RawBody = $resp.Content }
    } catch {
        $code = 0
        $rawBody = ""
        try {
            $code = [int]$_.Exception.Response.StatusCode
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $rawBody = $reader.ReadToEnd()
        } catch {}
        $parsed = $null
        try { $parsed = $rawBody | ConvertFrom-Json } catch {}
        return [PSCustomObject]@{ StatusCode = $code; Body = $parsed; RawBody = $rawBody }
    }
}

# -----------------------------------------------------------
Write-Title "0. Health Check"
# -----------------------------------------------------------
$r = Invoke-Api "GET" "/health"
Assert-Status "GET /health" $r 200

# -----------------------------------------------------------
Write-Title "1. Criar usuario de teste"
# -----------------------------------------------------------
$userBody = @{
    username = "testuser$(Get-Random -Maximum 9999)"
    password = "Test@1234"
    email    = "test$(Get-Random -Maximum 9999)@developerstore.com"
    phone    = "+5511999990000"
    role     = 3
    status   = 1
}
Write-Host "     Enviando: $($userBody | ConvertTo-Json -Compress)" -ForegroundColor Gray
$r = Invoke-Api "POST" "/api/users" $userBody
Assert-Status "POST /api/users" $r 201

# -----------------------------------------------------------
Write-Title "2. Autenticar e obter token JWT"
# -----------------------------------------------------------
$authBody = @{ email = $userBody.email; password = $userBody.password }
$r = Invoke-Api "POST" "/api/auth" $authBody
Assert-Status "POST /api/auth" $r 200

Write-Host "     Auth raw: $($r.RawBody)" -ForegroundColor DarkGray
$TOKEN = $r.Body.data.token
if (-not $TOKEN) { $TOKEN = $r.Body.Data.Token }
if (-not $TOKEN) {
    Write-Host "  [WARN] Token nao encontrado - endpoints autenticados podem falhar" -ForegroundColor Yellow
} else {
    Write-Host "     Token obtido com sucesso (len=$($TOKEN.Length))" -ForegroundColor Gray
}

# -----------------------------------------------------------
Write-Title "3. Criar venda (sem desconto - 2 itens)"
# -----------------------------------------------------------
$saleBody = @{
    customerId   = [guid]::NewGuid().ToString()
    customerName = "Cliente Teste"
    branchId     = [guid]::NewGuid().ToString()
    branchName   = "Filial Sao Paulo"
    saleDate     = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    items        = @(
        @{ productId = [guid]::NewGuid().ToString(); productName = "Produto A"; quantity = 2; unitPrice = 50.00 }
    )
}
$r = Invoke-Api "POST" "/api/sales" $saleBody $TOKEN
Assert-Status "POST /api/sales (2 itens, 0pct desconto)" $r 201
$SALE_ID = $r.Body.data.id
Write-Host "     SaleId: $SALE_ID" -ForegroundColor Gray

# -----------------------------------------------------------
Write-Title "4. Criar venda com 10pct de desconto (5 itens)"
# -----------------------------------------------------------
$saleBody2 = @{
    customerId   = [guid]::NewGuid().ToString()
    customerName = "Cliente Desconto 10"
    branchId     = [guid]::NewGuid().ToString()
    branchName   = "Filial Rio"
    saleDate     = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    items        = @(
        @{ productId = [guid]::NewGuid().ToString(); productName = "Produto B"; quantity = 5; unitPrice = 100.00 }
    )
}
$r = Invoke-Api "POST" "/api/sales" $saleBody2 $TOKEN
Assert-Status "POST /api/sales (5 itens, 10pct desconto)" $r 201
if ($r.Body.data.items) {
    $discount = $r.Body.data.items[0].discount
    if ($discount -eq 0.10) {
        Write-Host "  [PASS] Desconto 10pct aplicado corretamente" -ForegroundColor Green
        $script:PASS++
    } else {
        Write-Host "  [FAIL] Desconto esperado 0.10, recebido $discount" -ForegroundColor Red
        $script:FAIL++
    }
}

# -----------------------------------------------------------
Write-Title "5. Criar venda com 20pct de desconto (10 itens)"
# -----------------------------------------------------------
$saleBody3 = @{
    customerId   = [guid]::NewGuid().ToString()
    customerName = "Cliente Desconto 20"
    branchId     = [guid]::NewGuid().ToString()
    branchName   = "Filial BH"
    saleDate     = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    items        = @(
        @{ productId = [guid]::NewGuid().ToString(); productName = "Produto C"; quantity = 10; unitPrice = 30.00 }
    )
}
$r = Invoke-Api "POST" "/api/sales" $saleBody3 $TOKEN
Assert-Status "POST /api/sales (10 itens, 20pct desconto)" $r 201
if ($r.Body.data.items) {
    $discount = $r.Body.data.items[0].discount
    if ($discount -eq 0.20) {
        Write-Host "  [PASS] Desconto 20pct aplicado corretamente" -ForegroundColor Green
        $script:PASS++
    } else {
        Write-Host "  [FAIL] Desconto esperado 0.20, recebido $discount" -ForegroundColor Red
        $script:FAIL++
    }
}

# -----------------------------------------------------------
Write-Title "6. Tentar criar venda com mais de 20 itens (deve dar erro)"
# -----------------------------------------------------------
$saleBodyInvalid = @{
    customerId   = [guid]::NewGuid().ToString()
    customerName = "Cliente Invalido"
    branchId     = [guid]::NewGuid().ToString()
    branchName   = "Filial X"
    saleDate     = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    items        = @(
        @{ productId = [guid]::NewGuid().ToString(); productName = "Produto D"; quantity = 21; unitPrice = 10.00 }
    )
}
$r = Invoke-Api "POST" "/api/sales" $saleBodyInvalid $TOKEN
if ($r.StatusCode -in 400, 422) {
    Write-Host "  [PASS] POST /api/sales (21 itens) retornou $($r.StatusCode) - regra aplicada" -ForegroundColor Green
    $script:PASS++
} else {
    Write-Host "  [FAIL] POST /api/sales (21 itens) - esperado 400/422, recebido $($r.StatusCode)" -ForegroundColor Red
    if ($r.RawBody) { Write-Host "         Body: $($r.RawBody)" -ForegroundColor DarkRed }
    $script:FAIL++
}

# -----------------------------------------------------------
Write-Title "7. Buscar venda por ID"
# -----------------------------------------------------------
if ($SALE_ID) {
    $r = Invoke-Api "GET" "/api/sales/$SALE_ID" -token $TOKEN
    Assert-Status "GET /api/sales/{id}" $r 200
} else {
    Write-Host "  [SKIP] Sem SaleId - pulando GET por ID" -ForegroundColor Yellow
}

# -----------------------------------------------------------
Write-Title "8. Listar vendas (paginado)"
# -----------------------------------------------------------
$r = Invoke-Api "GET" "/api/sales?page=1&pageSize=10" -token $TOKEN
Assert-Status "GET /api/sales?page=1&pageSize=10" $r 200

# -----------------------------------------------------------
Write-Title "9. Atualizar venda"
# -----------------------------------------------------------
if ($SALE_ID) {
    $updateBody = @{
        customerId   = [guid]::NewGuid().ToString()
        customerName = "Cliente Atualizado"
        branchId     = [guid]::NewGuid().ToString()
        branchName   = "Filial Atualizada"
        saleDate     = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
        items        = @(
            @{ productId = [guid]::NewGuid().ToString(); productName = "Produto Atualizado"; quantity = 4; unitPrice = 25.00 }
        )
    }
    $r = Invoke-Api "PUT" "/api/sales/$SALE_ID" $updateBody $TOKEN
    Assert-Status "PUT /api/sales/{id}" $r 200
    if ($r.Body.data.items) {
        $discount = $r.Body.data.items[0].discount
        if ($discount -eq 0.10) {
            Write-Host "  [PASS] Desconto 10pct recalculado corretamente apos update" -ForegroundColor Green
            $script:PASS++
        } else {
            Write-Host "  [FAIL] Desconto esperado 0.10 apos update, recebido $discount" -ForegroundColor Red
            $script:FAIL++
        }
    }
}

# -----------------------------------------------------------
Write-Title "10. Cancelar item da venda"
# -----------------------------------------------------------
if ($SALE_ID) {
    $r = Invoke-Api "GET" "/api/sales/$SALE_ID" -token $TOKEN
    if ($r.Body.data.items) {
        $ITEM_ID = $r.Body.data.items[0].id
        if ($ITEM_ID) {
            $r = Invoke-Api "PATCH" "/api/sales/$SALE_ID/items/$ITEM_ID/cancel" -token $TOKEN
            Assert-Status "PATCH /api/sales/{id}/items/{itemId}/cancel" $r 200
        } else {
            Write-Host "  [SKIP] Item ID nao encontrado" -ForegroundColor Yellow
        }
    }
}

# -----------------------------------------------------------
Write-Title "11. Cancelar venda"
# -----------------------------------------------------------
if ($SALE_ID) {
    $r = Invoke-Api "PATCH" "/api/sales/$SALE_ID/cancel" -token $TOKEN
    Assert-Status "PATCH /api/sales/{id}/cancel" $r 200
}

# -----------------------------------------------------------
Write-Title "12. Deletar venda"
# -----------------------------------------------------------
if ($SALE_ID) {
    $r = Invoke-Api "DELETE" "/api/sales/$SALE_ID" -token $TOKEN
    Assert-Status "DELETE /api/sales/{id}" $r 200
}

# -----------------------------------------------------------
Write-Title "13. Buscar venda deletada (deve retornar 404)"
# -----------------------------------------------------------
if ($SALE_ID) {
    $r = Invoke-Api "GET" "/api/sales/$SALE_ID" -token $TOKEN
    Assert-Status "GET /api/sales/{id} apos delete (404 esperado)" $r 404
}

# -----------------------------------------------------------
Write-Title "14. Validacoes - campos obrigatorios ausentes"
# -----------------------------------------------------------
$r = Invoke-Api "POST" "/api/sales" @{} $TOKEN
if ($r.StatusCode -in 400, 422) {
    Write-Host "  [PASS] POST /api/sales sem body retornou $($r.StatusCode)" -ForegroundColor Green
    $script:PASS++
} else {
    Write-Host "  [FAIL] POST /api/sales sem body - esperado 400/422, recebido $($r.StatusCode)" -ForegroundColor Red
    $script:FAIL++
}

# -----------------------------------------------------------
# RESULTADO FINAL
# -----------------------------------------------------------
$TOTAL = $PASS + $FAIL
Write-Host "`n============================================" -ForegroundColor White
Write-Host "  RESULTADO: $PASS/$TOTAL testes passaram" -ForegroundColor $(if ($FAIL -eq 0) { "Green" } else { "Yellow" })
if ($ERRORS.Count -gt 0) {
    Write-Host "  Falhas:" -ForegroundColor Red
    $ERRORS | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
}
Write-Host "============================================`n" -ForegroundColor White
