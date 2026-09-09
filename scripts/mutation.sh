#!/usr/bin/env bash
set -euo pipefail
dotnet tool restore
dotnet stryker --config-file stryker-config.json --skip-version-check
