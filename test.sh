#!/usr/bin/env bash
set -euo pipefail

# ─────────────────────────────────────────────────────────────────────
#  ElasticMCP — Local Test Pipeline
#  Run: bash test.sh [--unit-only] [--integration-only] [--coverage]
# ─────────────────────────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

UNIT_ONLY=false
INTEGRATION_ONLY=false
COVERAGE=false

for arg in "$@"; do
  case $arg in
    --unit-only)       UNIT_ONLY=true ;;
    --integration-only) INTEGRATION_ONLY=true ;;
    --coverage)        COVERAGE=true ;;
    --help|-h)
      echo "Usage: bash test.sh [OPTIONS]"
      echo ""
      echo "Options:"
      echo "  --unit-only          Run only unit tests (no Docker needed)"
      echo "  --integration-only   Run only integration tests (Docker required)"
      echo "  --coverage           Collect code coverage (requires coverlet)"
      echo "  -h, --help           Show this help"
      exit 0
      ;;
  esac
done

step=0
pass=0
fail=0

run_step() {
  step=$((step + 1))
  echo ""
  echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
  echo -e "${CYAN}  Step $step: $1${NC}"
  echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

check_result() {
  if [ $1 -eq 0 ]; then
    echo -e "${GREEN}  ✓ $2${NC}"
    pass=$((pass + 1))
  else
    echo -e "${RED}  ✗ $2${NC}"
    fail=$((fail + 1))
  fi
}

# ── Step 1: Prerequisites ────────────────────────────────────────────
run_step "Check prerequisites"

if command -v dotnet &> /dev/null; then
  DOTNET_VERSION=$(dotnet --version)
  echo -e "  .NET SDK: ${GREEN}$DOTNET_VERSION${NC}"
else
  echo -e "${RED}  .NET SDK not found. Install from https://dotnet.microsoft.com/download${NC}"
  exit 1
fi

if [ "$UNIT_ONLY" = false ]; then
  if docker info &> /dev/null 2>&1; then
    echo -e "  Docker:   ${GREEN}Running${NC}"
  else
    echo -e "${YELLOW}  Docker is not running — integration tests will be skipped.${NC}"
    echo -e "${YELLOW}  Start Docker Desktop and re-run, or use --unit-only.${NC}"
    UNIT_ONLY=true
  fi
fi

# ── Step 2: Clean & Restore ──────────────────────────────────────────
run_step "Clean & restore packages"

dotnet clean ElasticMcp.slnx -v quiet 2>&1 | tail -1
dotnet restore ElasticMcp.slnx -v quiet 2>&1 | tail -1
check_result $? "Packages restored"

# ── Step 3: Build ────────────────────────────────────────────────────
run_step "Build solution"

dotnet build ElasticMcp.slnx --no-restore -v quiet 2>&1
check_result $? "Build succeeded"

# ── Step 4: Unit Tests ───────────────────────────────────────────────
if [ "$INTEGRATION_ONLY" = false ]; then
  run_step "Unit tests"

  if [ "$COVERAGE" = true ]; then
    dotnet test tests/ElasticMcp.Tests/ --no-build \
      --collect:"XPlat Code Coverage" \
      --results-directory ./test-results/unit \
      --verbosity normal 2>&1
  else
    dotnet test tests/ElasticMcp.Tests/ --no-build --verbosity normal 2>&1
  fi
  check_result $? "Unit tests"
fi

# ── Step 5: Integration Tests ────────────────────────────────────────
if [ "$UNIT_ONLY" = false ]; then
  run_step "Integration tests (Testcontainers + Elasticsearch 9.x)"
  echo -e "  ${YELLOW}This may take 1-2 minutes on first run (Docker image pull)${NC}"

  if [ "$COVERAGE" = true ]; then
    dotnet test tests/ElasticMcp.IntegrationTests/ --no-build \
      --collect:"XPlat Code Coverage" \
      --results-directory ./test-results/integration \
      --verbosity normal 2>&1
  else
    dotnet test tests/ElasticMcp.IntegrationTests/ --no-build --verbosity normal 2>&1
  fi
  check_result $? "Integration tests"
fi

# ── Summary ──────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${CYAN}  Summary${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "  Steps passed: ${GREEN}$pass${NC}"
if [ $fail -gt 0 ]; then
  echo -e "  Steps failed: ${RED}$fail${NC}"
  echo ""
  echo -e "${RED}  Pipeline FAILED${NC}"
  exit 1
else
  echo ""
  echo -e "${GREEN}  All checks passed!${NC}"
fi
