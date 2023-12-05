#!/bin/bash

dotnet publish -c Release -r osx-x64 -p:PublishReadyToRun=true --self-contained
dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true --self-contained
#dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true --self-contained

cd "bin/"

mkdir -p "TouchPortalTeamsIntegrationPlugin"
mkdir -p "TouchPortalTeamsIntegrationPlugin/osx"
mkdir -p "TouchPortalTeamsIntegrationPlugin/win"
#mkdir -p "TouchPortalTeamsIntegrationPlugin/linux"

cp -R "Release/net7.0/osx-x64/publish/" "TouchPortalTeamsIntegrationPlugin/osx/"
cp -R "Release/net7.0/win-x64/publish/" "TouchPortalTeamsIntegrationPlugin/win/"
#cp -R "Release/net7.0/linux-x64/publish/" "TouchPortalTeamsIntegrationPlugin/linux/"

cp "Release/net7.0/osx-x64/publish/icon24.png" "TouchPortalTeamsIntegrationPlugin/"
cp "Release/net7.0/osx-x64/publish/entry.tp" "TouchPortalTeamsIntegrationPlugin/"

rm "../Touch_Portal_Teams_Integration_Plugin.tpp"
zip -q -r "../Touch_Portal_Teams_Integration_Plugin.tpp" "TouchPortalTeamsIntegrationPlugin" -x "*.DS_Store"
rm -R "TouchPortalTeamsIntegrationPlugin"

cd ".."
open .