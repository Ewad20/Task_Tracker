# Task Tracker — Makefile
# Użycie: make [cel]
# Wymagania: .NET 9 SDK, Docker Desktop, make (np. via Chocolatey: choco install make)
#
# Dostępne cele:
#   make all         — clean + build + test + docs
#   make build       — restore + build (Release)
#   make test        — uruchom testy jednostkowe
#   make docs        — generuj Swagger JSON + XML docs
#   make clean       — usuń artefakty i binaria
#   make docker-up   — uruchom wszystkie kontenery Docker
#   make docker-down — zatrzymaj kontenery Docker
#   make help        — wyświetl tę pomoc

SOLUTION     := backend.slnf
ARTIFACTS    := artifacts
TEST_RESULTS := $(ARTIFACTS)/test-results
SWAGGER_DOCS := docs/swagger
DOTNET       := dotnet
DOCKER       := docker compose

.PHONY: all build test docs clean docker-up docker-down help

all: clean build test docs

build:
	@echo ">>> Przywracanie pakietów NuGet..."
	$(DOTNET) restore $(SOLUTION) --nologo
	@echo ">>> Budowanie rozwiązania (Release)..."
	$(DOTNET) build $(SOLUTION) --no-restore --nologo -c Release

test:
	@echo ">>> Uruchamianie testów jednostkowych..."
	@mkdir -p $(TEST_RESULTS)
	$(DOTNET) test $(SOLUTION) \
		--no-build \
		--nologo \
		-c Release \
		--logger "trx;LogFileName=test-results.trx" \
		--results-directory $(TEST_RESULTS) \
		--verbosity normal
	@echo ">>> Wyniki testów: $(TEST_RESULTS)/test-results.trx"

docs:
	@echo ">>> Generowanie dokumentacji Swagger JSON..."
	@mkdir -p $(SWAGGER_DOCS)
	$(DOTNET) tool install --global Swashbuckle.AspNetCore.Cli --version 6.9.0 2>/dev/null || true
	$(DOTNET) swagger tofile --output $(SWAGGER_DOCS)/UserService-swagger.json \
		src/Services/UserService/bin/Release/net9.0/UserService.dll v1 || true
	$(DOTNET) swagger tofile --output $(SWAGGER_DOCS)/ProjectService-swagger.json \
		src/Services/ProjectService/bin/Release/net9.0/ProjectService.dll v1 || true
	$(DOTNET) swagger tofile --output $(SWAGGER_DOCS)/TaskService-swagger.json \
		src/Services/TaskService/bin/Release/net9.0/TaskService.dll v1 || true
	$(DOTNET) swagger tofile --output $(SWAGGER_DOCS)/NotificationService-swagger.json \
		src/Services/NotificationService/bin/Release/net9.0/NotificationService.dll v1 || true
	$(DOTNET) swagger tofile --output $(SWAGGER_DOCS)/AuditService-swagger.json \
		src/Services/AuditService/bin/Release/net9.0/AuditService.dll v1 || true
	$(DOTNET) swagger tofile --output $(SWAGGER_DOCS)/ReportingService-swagger.json \
		src/Services/ReportingService/bin/Release/net9.0/ReportingService.dll v1 || true
	@echo ">>> Dokumentacja zapisana w: $(SWAGGER_DOCS)"

clean:
	@echo ">>> Czyszczenie artefaktów..."
	$(DOTNET) clean $(SOLUTION) --nologo -v minimal
	@rm -rf $(ARTIFACTS)

docker-up:
	@echo ">>> Uruchamianie Docker Compose..."
	$(DOCKER) up --build -d

docker-down:
	@echo ">>> Zatrzymywanie Docker Compose..."
	$(DOCKER) down

help:
	@echo ""
	@echo "Task Tracker — dostępne cele Make:"
	@echo "  make all         — clean + build + test + docs"
	@echo "  make build       — przywróć pakiety i zbuduj"
	@echo "  make test        — uruchom testy jednostkowe"
	@echo "  make docs        — generuj Swagger JSON"
	@echo "  make clean       — usuń artefakty"
	@echo "  make docker-up   — uruchom Docker Compose"
	@echo "  make docker-down — zatrzymaj Docker Compose"
	@echo ""
