#!/usr/bin/env bash
set -euo pipefail

display_number="${PLAYWRIGHT_DISPLAY_NUMBER:-50}"
display=":${display_number}"
rfb_port=$((5900 + display_number))
export DISPLAY="$display"

Xvfb "$DISPLAY" -screen 0 1280x900x24 -nolisten tcp -ac &
pids=("$!")

cleanup() {
  kill "${pids[@]}" 2>/dev/null || true
}
trap cleanup EXIT
trap 'exit 0' INT TERM

for _ in $(seq 1 50); do
  if xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
    break
  fi
  sleep 0.1
done

if ! xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
  echo "Xvfb did not start on display $DISPLAY" >&2
  exit 1
fi

fluxbox >/tmp/fluxbox.log 2>&1 &
pids+=("$!")

x11vnc -display "$DISPLAY" -forever -shared -nopw -localhost -rfbport "$rfb_port" &
pids+=("$!")

websockify --web=/usr/share/novnc 6080 "127.0.0.1:${rfb_port}" &
pids+=("$!")

dotnet /app/publish/Collector.Playwright.dll
