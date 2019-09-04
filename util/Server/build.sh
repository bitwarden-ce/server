#!/usr/bin/env bash
set -e

DIR=$(realpath $(dirname ${BASH_SOURCE[0]}))
cd $DIR

echo -e "\n## Building Server"

echo -e "\nBuilding app"
echo ".NET Core version $(dotnet --version)"
echo "Restore"
dotnet restore $DIR/Server.csproj
echo "Clean"
dotnet clean $DIR/Server.csproj -c "Release" -o $DIR/obj/Docker/publish
echo "Publish"
dotnet publish $DIR/Server.csproj -c "Release" -o $DIR/obj/Docker/publish

echo -e "\nBuilding docker image"
docker --version
docker build -t bitwardence/server $DIR/.
