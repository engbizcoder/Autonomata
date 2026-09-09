#!/usr/bin/env bash
set -euo pipefail
dotnet test Autonomata.slnx --no-build --configuration Release
