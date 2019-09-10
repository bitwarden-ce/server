#!/usr/bin/env bash
set -e

DIR="$(dirname "${BASH_SOURCE[0]}")"
cd ${DIR}

echo -e "\n## Building MsSql"

echo -e "\nBuilding docker image"
docker --version
docker build -t bytegarden/mssql .
