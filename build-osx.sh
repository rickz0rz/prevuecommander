#!/bin/sh
# Builds a universal macOS program, PrevueCommander-osx, for Apple silicon and Intel.
# Do not trim the output: YamlDotNet and XmlSerializer need the metadata that trimming removes.
set -eu

for rid in osx-x64 osx-arm64; do
    dotnet publish PrevueCommander/PrevueCommander.csproj -c Release -r "$rid" --self-contained true \
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true \
        -o "publish_$rid"
done

lipo -create -output PrevueCommander-osx publish_osx-arm64/PrevueCommander publish_osx-x64/PrevueCommander
