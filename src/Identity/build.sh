#!/usr/bin/env bash
set -e

DIR=$(realpath $(dirname ${BASH_SOURCE[0]}))
cd $DIR

echo -e "\n## Building Identity"

echo -e "\nBuilding app"
echo ".NET Core version $(dotnet --version)"
echo "Restore"
dotnet restore $DIR/Identity.csproj
echo "Clean"
dotnet clean $DIR/Identity.csproj -c "Release" -o $DIR/obj/Docker/publish
echo "Publish"
dotnet publish $DIR/Identity.csproj -c "Release" -o $DIR/obj/Docker/publish

echo -e "\nBuilding docker image"
docker --version
docker build -t bytegarden/identity $DIR/.
