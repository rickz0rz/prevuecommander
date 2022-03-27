#!/bin/bash

# Build x64
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true /p:PublishTrimmed=true --self-contained true -o publish_osx-x64 PrevueCommander/PrevueCommander.csproj

# Build ARM
dotnet publish -r osx-arm64 -c Release /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishReadyToRun=true /p:PublishTrimmed=true --self-contained true -o publish_osx-arm64 PrevueCommander/PrevueCommander.csproj

# Lipo to combine to universal binary
lipo -create -output PrevueCommander-osx publish_osx-arm64/PrevueCommander publish_osx-x64/PrevueCommander


