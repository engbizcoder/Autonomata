#!/usr/bin/env bash
set -euo pipefail
dotnet pack src/Autonomata/Autonomata.csproj --no-build --configuration Release --output artifacts/packages
