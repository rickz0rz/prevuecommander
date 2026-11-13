#!/bin/sh
# Builds a Linux arm64 program in publish_linux-arm64, for example for a Raspberry Pi with a 64-bit OS.
# Do not trim the output: YamlDotNet and XmlSerializer need the metadata that trimming removes.
set -eu

dotnet publish PrevueCommander/PrevueCommander.csproj -c Release -r linux-arm64 --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true \
    -o publish_linux-arm64
