#!/usr/bin/env bash
set -euo pipefail
dotnet test tests/Autonomata.Tests/Autonomata.Tests.csproj --configuration Release --no-build \
  /p:CollectCoverage=true \
  /p:CoverletOutput=../../artifacts/coverage/ \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=90 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total
