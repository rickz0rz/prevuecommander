#!/bin/sh
# Builds a Windows x64 program in publish_win-x64.
# Do not trim the output: YamlDotNet and XmlSerializer need the metadata that trimming removes.
set -eu

dotnet publish PrevueCommander/PrevueCommander.csproj -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true \
    -o publish_win-x64
