#!/usr/bin/env bash
set -euo pipefail
dotnet build Autonomata.slnx --no-restore --configuration Release
