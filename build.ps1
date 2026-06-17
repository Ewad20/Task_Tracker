<#
.SYNOPSIS
    Skrypt automatyzujacy budowanie, testowanie i generowanie dokumentacji
    dla projektu Task Tracker.

.DESCRIPTION
    Uruchamia kolejno:
      1. Przywracanie zaleznosci NuGet
      2. Budowanie rozwiazania
      3. Uruchamianie testow jednostkowych z raportem wynikow
      4. Generowanie dokumentacji Swagger JSON dla kazdego serwisu
      5. Kopiowanie XML docs do katalogu docs/swagger/

.PARAMETER Target
    Cel budowania: All (domyslnie), Build, Test, Docs, Clean, FetchSwagger

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Target Test
    .\build.ps1 -Target Docs
    .\build.ps1 -Target Clean
    .\build.ps1 -Target Build
    .\build.ps1 -Target FetchSwagger
#>

param(
    [ValidateSet("All", "Build", "Test", "Docs", "Clean", "FetchSwagger")]
    [string]$Target = "All"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SolutionFile  = "backend.slnf"
$ArtifactsDir  = "artifacts"
$DocsDir       = "docs/swagger"
$TestResultDir = "artifacts/test-results"

# -----------------------------------------------------------------
# Helpers
# -----------------------------------------------------------------
function Write-Header([string]$Msg) {
    Write-Host ""
    Write-Host ("=" * 50) -ForegroundColor Cyan
    Write-Host "  $Msg" -ForegroundColor Cyan
    Write-Host ("=" * 50) -ForegroundColor Cyan
}

function Invoke-Step([string]$Name, [scriptblock]$Action) {
    Write-Header $Name
    try {
        & $Action
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
            throw "Komenda zakonczyla sie kodem $LASTEXITCODE"
        }
        Write-Host "[OK] $Name" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERROR] $Name - BLAD: $_" -ForegroundColor Red
        exit 1
    }
}

# -----------------------------------------------------------------
# Steps
# -----------------------------------------------------------------
function Invoke-Clean {
    Invoke-Step "Czyszczenie artefaktow i binariow" {
        if (Test-Path $ArtifactsDir) { Remove-Item -Recurse -Force $ArtifactsDir }
        dotnet clean $SolutionFile --nologo -v minimal
    }
}

function Invoke-Restore {
    Invoke-Step "Przywracanie pakietow NuGet" {
        dotnet restore $SolutionFile --nologo
    }
}

function Invoke-Build {
    Invoke-Step "Budowanie rozwiazania (Release)" {
        dotnet build $SolutionFile --no-restore --nologo -c Release
    }
}

function Invoke-Test {
    Invoke-Step "Uruchamianie testow jednostkowych" {
        New-Item -ItemType Directory -Force -Path $TestResultDir | Out-Null
        dotnet test $SolutionFile `
            --no-build `
            --nologo `
            -c Release `
            --logger "trx;LogFileName=test-results.trx" `
            --results-directory $TestResultDir `
            --verbosity normal
        Write-Host ""
        Write-Host "  Wyniki testow: $TestResultDir/test-results.trx" -ForegroundColor Yellow
    }
}

function Invoke-Docs {
    Invoke-Step "Generowanie dokumentacji (Swagger JSON + XML)" {
        New-Item -ItemType Directory -Force -Path $DocsDir | Out-Null

        Write-Host "  Kopiowanie dokumentacji XML..." -ForegroundColor Yellow

        $services = @(
            @{ Name = "UserService";         Dll = "src/Services/UserService/bin/Release/net9.0/UserService.dll" },
            @{ Name = "ProjectService";      Dll = "src/Services/ProjectService/bin/Release/net9.0/ProjectService.dll" },
            @{ Name = "TaskService";         Dll = "src/Services/TaskService/bin/Release/net9.0/TaskService.dll" },
            @{ Name = "NotificationService"; Dll = "src/Services/NotificationService/bin/Release/net9.0/NotificationService.dll" },
            @{ Name = "AuditService";        Dll = "src/Services/AuditService/bin/Release/net9.0/AuditService.dll" },
            @{ Name = "ReportingService";    Dll = "src/Services/ReportingService/bin/Release/net9.0/ReportingService.dll" }
        )

        $xmlCopied = 0
        foreach ($svc in $services) {
            # XML docs (kopia obok swagger.json)
            $xmlSrc = $svc.Dll -replace "\.dll$", ".xml"
            if (Test-Path $xmlSrc) {
                Copy-Item $xmlSrc "$DocsDir/$($svc.Name).xml" -Force
                $xmlCopied++
            }
        }

        if ($xmlCopied -gt 0) {
            Write-Host "    Skopiowano $xmlCopied plikow XML" -ForegroundColor DarkGreen
        }

        Write-Host ""
        Write-Host "  UWAGA: Pliki swagger.json sa generowane w runtime przez kazdy serwis." -ForegroundColor Cyan
        Write-Host "  Aby pobrac swagger.json, uruchom serwisy i uzyj endpointow:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "    - http://localhost:8080/api/users/swagger/v1/swagger.json" -ForegroundColor Gray
        Write-Host "    - http://localhost:8080/api/projects/swagger/v1/swagger.json" -ForegroundColor Gray
        Write-Host "    - http://localhost:8080/api/tasks/swagger/v1/swagger.json" -ForegroundColor Gray
        Write-Host "    - http://localhost:8080/api/notifications/swagger/v1/swagger.json" -ForegroundColor Gray
        Write-Host "    - http://localhost:8080/api/audit/swagger/v1/swagger.json" -ForegroundColor Gray
        Write-Host "    - http://localhost:8080/api/reports/swagger/v1/swagger.json" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Lub uruchom: .\build.ps1 -Target FetchSwagger (wymaga dzialajacych serwisow)" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Dokumentacja XML zapisana w: $DocsDir" -ForegroundColor Yellow
    }
}

function Invoke-FetchSwagger {
    Invoke-Step "Pobieranie dokumentacji Swagger z dzialajacych serwisow" {
        New-Item -ItemType Directory -Force -Path $DocsDir | Out-Null

        $gatewayUrl = "http://localhost:8080"

        # Sprawdz czy gateway jest dostepny
        try {
            $null = Invoke-WebRequest -Uri "$gatewayUrl/health" -TimeoutSec 2 -ErrorAction Stop
        }
        catch {
            throw "API Gateway nie jest dostepny pod adresem $gatewayUrl. Uruchom serwisy przez Docker: docker-compose up -d"
        }

        $endpoints = @(
            @{ Name = "UserService";         Path = "/api/users/swagger/v1/swagger.json" },
            @{ Name = "ProjectService";      Path = "/api/projects/swagger/v1/swagger.json" },
            @{ Name = "TaskService";         Path = "/api/tasks/swagger/v1/swagger.json" },
            @{ Name = "NotificationService"; Path = "/api/notifications/swagger/v1/swagger.json" },
            @{ Name = "AuditService";        Path = "/api/audit/swagger/v1/swagger.json" },
            @{ Name = "ReportingService";    Path = "/api/reports/swagger/v1/swagger.json" }
        )

        $fetched = 0
        foreach ($ep in $endpoints) {
            Write-Host "  Pobieranie Swagger dla: $($ep.Name)..." -ForegroundColor Yellow
            try {
                $url = "$gatewayUrl$($ep.Path)"
                $output = "$DocsDir/$($ep.Name)-swagger.json"
                Invoke-WebRequest -Uri $url -OutFile $output -TimeoutSec 10 -ErrorAction Stop
                if (Test-Path $output) {
                    $size = (Get-Item $output).Length
                    Write-Host "    OK ($size bytes)" -ForegroundColor DarkGreen
                    $fetched++
                }
            }
            catch {
                Write-Host "    BLAD: $_" -ForegroundColor Red
            }
        }

        Write-Host ""
        if ($fetched -eq $endpoints.Count) {
            Write-Host "  Pobrano $fetched/$($endpoints.Count) plikow swagger.json" -ForegroundColor Green
        }
        else {
            Write-Host "  Pobrano $fetched/$($endpoints.Count) plikow (niektorych serwisow brak)" -ForegroundColor Yellow
        }
        Write-Host "  Dokumentacja zapisana w: $DocsDir" -ForegroundColor Yellow
    }
}

# -----------------------------------------------------------------
# Main
# -----------------------------------------------------------------
$t0 = Get-Date
Write-Host ""
Write-Host "Task Tracker - Build Script  [Cel: $Target]" -ForegroundColor Magenta
Write-Host "Start: $t0" -ForegroundColor DarkGray

switch ($Target) {
    "Clean"        { Invoke-Clean }
    "Build"        { Invoke-Restore; Invoke-Build }
    "Test"         { Invoke-Restore; Invoke-Build; Invoke-Test }
    "Docs"         { Invoke-Restore; Invoke-Build; Invoke-Docs }
    "FetchSwagger" { Invoke-FetchSwagger }
    "All"          { Invoke-Clean; Invoke-Restore; Invoke-Build; Invoke-Test; Invoke-Docs }
}

$elapsed = (Get-Date) - $t0
Write-Host ""
$elapsedFormatted = "{0:mm\:ss\.ff}" -f $elapsed
Write-Host "Zakonczono [$Target] w $elapsedFormatted" -ForegroundColor Green
