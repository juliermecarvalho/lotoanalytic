.PHONY: test test-scripts test-api test-api-unit test-api-integration build-api

test: test-scripts test-api test-api-unit build-api

test-scripts:
	bash scripts/tests/common.test.sh
	bash scripts/tests/monorepo_structure.test.sh

test-api:
	pwsh -NoProfile -File scripts/tests/api_smoke.test.ps1
	pwsh -NoProfile -File scripts/tests/api_docs_smoke.test.ps1

test-api-unit:
	dotnet test --project apps/api/tests/Unit/LotoAnalytics.Api.UnitTests.csproj

test-api-integration:
	DOCKER_API_VERSION=1.43 dotnet test --project apps/api/tests/Integration/LotoAnalytics.Api.IntegrationTests.csproj --no-restore

build-api:
	dotnet build LotoAnalytics.slnx -nologo
