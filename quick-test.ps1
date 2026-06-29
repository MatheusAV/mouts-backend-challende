$BASE = "http://localhost:8080"

function Call($method, $path, $body=$null, $token=$null) {
    $h = @{"Content-Type"="application/json"}
    if ($token) { $h["Authorization"] = "Bearer $token" }
    $params = @{ Method=$method; Uri="$BASE$path"; Headers=$h; UseBasicParsing=$true }
    if ($body) { $params["Body"] = ($body | ConvertTo-Json -Depth 10) }
    try {
        $r = Invoke-WebRequest @params
        return ($r.Content | ConvertFrom-Json)
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        return ($reader.ReadToEnd() | ConvertFrom-Json)
    }
}

# 1. Criar usuario
$user = @{ username="demo_$(Get-Random -Max 999)"; password="Test@1234"; email="demo$(Get-Random -Max 999)@test.com"; phone="+5511999990000"; role=3; status=1 }
$r = Call "POST" "/api/users" $user
Write-Host "=== POST /api/users ===" -ForegroundColor Cyan
Write-Host ($r | ConvertTo-Json -Depth 3)

# 2. Auth
$auth = @{ email=$user.email; password=$user.password }
$r = Call "POST" "/api/auth" $auth
Write-Host "`n=== POST /api/auth ===" -ForegroundColor Cyan
Write-Host ($r | ConvertTo-Json -Depth 4)
$token = $r.data.data.token

# 3. Criar venda
$sale = @{
    customerId=[guid]::NewGuid().ToString(); customerName="Empresa XPTO"
    branchId=[guid]::NewGuid().ToString(); branchName="Filial SP"
    saleDate=(Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    items=@(@{ productId=[guid]::NewGuid().ToString(); productName="Notebook Dell"; quantity=5; unitPrice=3500.00 })
}
$r = Call "POST" "/api/sales" $sale $token
Write-Host "`n=== POST /api/sales (5 itens = 10% desconto) ===" -ForegroundColor Cyan
Write-Host ($r | ConvertTo-Json -Depth 4)
$saleId = $r.data.id

# 4. GET por ID
$r = Call "GET" "/api/sales/$saleId" -token $token
Write-Host "`n=== GET /api/sales/{id} ===" -ForegroundColor Cyan
Write-Host ($r | ConvertTo-Json -Depth 4)

# 5. Listar
$r = Call "GET" "/api/sales?page=1&pageSize=5" -token $token
Write-Host "`n=== GET /api/sales (lista) ===" -ForegroundColor Cyan
Write-Host ($r | ConvertTo-Json -Depth 3)

# 6. Cancelar
$r = Call "PATCH" "/api/sales/$saleId/cancel" -token $token
Write-Host "`n=== PATCH /api/sales/{id}/cancel ===" -ForegroundColor Cyan
Write-Host ($r | ConvertTo-Json -Depth 3)
