#!/usr/bin/env bash
# Generate Kiosa app icons from SVG source in Assets/brand/.
#
# Prerequisites (macOS):
#   brew install librsvg imagemagick
#
# Outputs (committed to repo — Windows CI does not need SVG tooling):
#   src/EventPlatform.PrintRelay.App/Assets/brand/app.ico (16, 32, 48, 256)
#   src/EventPlatform.PrintRelay.App/Assets/brand/tray/base-32.png (monochrome icon-only — tray overlays)
#
# Sources (copied from kiosa-marketing/brand-pack/ into Assets/brand/):
#   kiosa-logo-icon.svg — app.ico / exe / forms
#   kiosa-tray-icon.svg — tray base (monochrome, no amber accent; brand pack §3)

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BRAND_DIR="$ROOT/src/EventPlatform.PrintRelay.App/Assets/brand"
ICON_SVG="$BRAND_DIR/kiosa-logo-icon.svg"
TRAY_SVG="$BRAND_DIR/kiosa-tray-icon.svg"
TRAY_DIR="$BRAND_DIR/tray"
TMP_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "error: $1 not found. Install with: brew install librsvg imagemagick" >&2
    exit 1
  fi
}

require_cmd rsvg-convert
require_cmd magick

if [[ ! -f "$ICON_SVG" ]]; then
  echo "error: missing $ICON_SVG — copy kiosa-logo-icon.svg from kiosa-marketing/brand-pack first." >&2
  exit 1
fi

if [[ ! -f "$TRAY_SVG" ]]; then
  echo "error: missing $TRAY_SVG — icon-only monochrome tray source." >&2
  exit 1
fi

mkdir -p "$TRAY_DIR"

for size in 16 32 48 256; do
  rsvg-convert -w "$size" -h "$size" "$ICON_SVG" -o "$TMP_DIR/icon-${size}.png"
done

rsvg-convert -w 32 -h 32 "$TRAY_SVG" -o "$TRAY_DIR/base-32.png"

magick "$TMP_DIR/icon-16.png" "$TMP_DIR/icon-32.png" "$TMP_DIR/icon-48.png" "$TMP_DIR/icon-256.png" "$BRAND_DIR/app.ico"

echo "Generated:"
echo "  $BRAND_DIR/app.ico"
echo "  $TRAY_DIR/base-32.png"
