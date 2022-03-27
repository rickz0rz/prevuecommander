#!/bin/bash

# Build x64
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true /p:PublishTrimmed=true --self-contained true -o publish_win-x64 PrevueCommander/PrevueCommander.csproj



