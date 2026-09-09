#!/usr/bin/env bash
set -euo pipefail
dotnet format Autonomata.slnx --no-restore --verify-no-changes
