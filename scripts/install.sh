#!/usr/bin/env sh

set -eu

VERSION="${VERSION:-latest}"
REPO="${REPO:-maffaciolli/MdPdf}"
INSTALL_ROOT="${INSTALL_ROOT:-$HOME/.local/share/mdpdf}"
BIN_DIR="${BIN_DIR:-$HOME/.local/bin}"
PACKAGE_NAME="mdpdf-linux-x64.tar.gz"
PACKAGED_EXE="MdPdf.Console"

command -v curl >/dev/null 2>&1 || {
  echo "Error: curl is required." >&2
  exit 1
}

command -v tar >/dev/null 2>&1 || {
  echo "Error: tar is required." >&2
  exit 1
}

TMP_DIR="$(mktemp -d)"
ARCHIVE_PATH="$TMP_DIR/$PACKAGE_NAME"
EXTRACT_DIR="$TMP_DIR/extract"
CURRENT_DIR="$INSTALL_ROOT/current"
DOWNLOAD_URL="https://github.com/$REPO/releases/latest/download/$PACKAGE_NAME"

cleanup() {
  rm -rf "$TMP_DIR"
}

trap cleanup EXIT INT HUP TERM

if [ "$VERSION" != "latest" ]; then
  case "$VERSION" in
    v*)
      TAG="$VERSION"
      ;;
    *)
      TAG="v$VERSION"
      ;;
  esac

  DOWNLOAD_URL="https://github.com/$REPO/releases/download/$TAG/$PACKAGE_NAME"
fi

mkdir -p "$BIN_DIR" "$INSTALL_ROOT" "$EXTRACT_DIR"

curl -fsSL "$DOWNLOAD_URL" -o "$ARCHIVE_PATH"
tar -xzf "$ARCHIVE_PATH" -C "$EXTRACT_DIR"

rm -rf "$CURRENT_DIR"
mv "$EXTRACT_DIR" "$CURRENT_DIR"

ln -sfn "$CURRENT_DIR/$PACKAGED_EXE" "$BIN_DIR/mdpdf"

echo "Installed to: $CURRENT_DIR"
echo "Command symlink: $BIN_DIR/mdpdf"

case ":$PATH:" in
  *":$BIN_DIR:"*) ;;
  *)
    echo "PATH guidance: add $BIN_DIR to PATH, for example:"
    echo "export PATH=\"$BIN_DIR:\$PATH\""
    ;;
esac

echo "Browser prerequisite: MdPdf requires Chrome, Edge, or Chromium already installed."
