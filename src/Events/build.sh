#!/usr/bin/env bash
set -e

DIR=$(realpath $(dirname ${BASH_SOURCE[0]}))
cd $DIR

echo -e "\n## Building Events"

echo -e "\nBuilding app"
echo ".NET Core version $(dotnet --version)"
echo "Restore"
dotnet restore $DIR/Events.csproj
echo "Clean"
dotnet clean $DIR/Events.csproj -c "Release" -o $DIR/obj/Docker/publish/Events
echo "Publish"
dotnet publish $DIR/Events.csproj -c "Release" -o $DIR/obj/Docker/publish/Events

echo -e "\nBuilding docker image"
docker --version
docker build -t bitwarden/events $DIR/.
