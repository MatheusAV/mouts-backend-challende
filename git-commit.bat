@echo off
echo Removendo lock do git (se existir)...
cd /d "%~dp0"
if exist ".git\index.lock" (
    del /f ".git\index.lock"
    echo Lock removido.
)
echo Adicionando arquivos...
git add -A
echo Fazendo commit...
git commit -m "fix: cria tabelas Sales/SaleItems via SQL idempotente no startup e corrige schema de Users"
echo.
echo Push para branch feat/sales-api...
git push -u origin feat/sales-api
echo.
echo Concluido!
pause
