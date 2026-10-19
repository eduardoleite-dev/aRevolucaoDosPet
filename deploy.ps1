$ErrorActionPreference = "Stop"

# ==========================================
# VARIÁVEIS DE CONFIGURAÇÃO (Ajuste aqui)
# ==========================================
$AppName       = "aRevolucaoDosPet"
$ResourceGroup = "pet_group"
$DllName       = "aRevolucaoDosPet.dll"
$PublishPath   = "./publish"
$ZipName       = "./app.zip"
$Configuration = "Release"

Write-Host "=== Iniciando o processo de Build e Deploy para o Azure (App Service) ===" -ForegroundColor Cyan

# 1. Limpa e publica a aplicação
Write-Host "Compilando e publicando o projeto (.NET $Configuration)..." -ForegroundColor Yellow
dotnet clean
dotnet publish -c $Configuration -o $PublishPath

# 2. Valida se os arquivos principais (especialmente a DLL principal) foram gerados no publish
$TargetDll = Join-Path $PublishPath $DllName
Write-Host "Verificando a existência do arquivo executável '$TargetDll'..." -ForegroundColor Yellow

if (-not (Test-Path $TargetDll)) {
    Write-Host "ERRO CRÍTICO: O arquivo principal '$DllName' não foi encontrado na pasta $PublishPath!" -ForegroundColor Red
    Write-Host "Verifique os erros de compilação do dotnet publish acima." -ForegroundColor Red
    exit 1
}

Write-Host "Arquivo '$DllName' encontrado com sucesso!" -ForegroundColor Green

# 3. Remove zip antigo se existir para evitar conflitos
if (Test-Path $ZipName) {
    Remove-Item $ZipName -Force
}

# 4. Compacta os arquivos gerados
Write-Host "Compactando os arquivos em $ZipName..." -ForegroundColor Yellow
Compress-Archive -Path "$PublishPath/*" -DestinationPath $ZipName -Force

# 5. Envia para o Azure App Service capturando possíveis erros de deploy
Write-Host "Enviando pacote para o Azure App Service ($AppName)..." -ForegroundColor Yellow

try {
    # Executa o deploy via Azure CLI
    az webapp deployment source config-zip --name $AppName --resource-group $ResourceGroup --src $ZipName
    Write-Host "=== Deploy finalizado com sucesso! ===" -ForegroundColor Green
}
catch {
    Write-Host "ERRO DURANTE O DEPLOY NO AZURE!" -ForegroundColor Red
    Write-Host "Detalhes do erro do Azure CLI: $_" -ForegroundColor Red
    
    Write-Host "Tentando resgatar os logs de diagnóstico do Kudu para análise..." -ForegroundColor Yellow
    
    # Obtém as credenciais dinamicamente para autenticar no Kudu
    $credsJson = az webapp deployment list-publishing-credentials --name $AppName --resource-group $ResourceGroup | ConvertFrom-Json
    $username = $credsJson.publishingUserName
    $password = $credsJson.publishingPassword
    $scmUri = $credsJson.scmUri

    # Monta a URL de logs do Kudu
    $logUri = "$scmUri/api/logstream"
    
    Write-Host "Para inspecionar os logs de erro ao vivo no Kudu, acesse o painel ou use o endpoint de logscm." -ForegroundColor Cyan
    Write-Host "Dica: Verifique se o comando de inicialização no Azure aponta corretamente para 'dotnet $DllName'." -ForegroundColor Yellow
    
    exit 1
}