#!/usr/bin/env bash

set -e
set -o
set -u
set pipefail

cd build

mkdir -p CommitDock.app/Contents/Resources
mv CommitDock CommitDock.app/Contents/MacOS
cp resources/app/App.icns CommitDock.app/Contents/Resources/App.icns
sed "s/APP_VERSION/$VERSION/g" resources/app/App.plist > CommitDock.app/Contents/Info.plist
rm -rf CommitDock.app/Contents/MacOS/CommitDock.dsym
rm -f CommitDock.app/Contents/MacOS/*.pdb

case "$RUNTIME" in
  osx-arm64) ARCH=arm64 ;;
  osx-x64)   ARCH=x86_64 ;;
  *) echo "Unsupported RUNTIME: $RUNTIME" >&2; exit 1 ;;
esac

clang -arch "$ARCH" ../tools/setsid-macos/setsid.c -o CommitDock.app/Contents/MacOS/setsid -mmacosx-version-min=13.0
codesign --force --deep --sign - CommitDock.app
codesign --verify --deep --strict --verbose=2 CommitDock.app

zip "commitdock_$VERSION.$RUNTIME.zip" -r CommitDock.app
