#!/usr/bin/env bash
set -euo pipefail

display_number="${PLAYWRIGHT_DISPLAY_NUMBER:-50}"
display=":${display_number}"
rfb_port=$((5900 + display_number))
x_lock="/tmp/.X${display_number}-lock"
x_socket="/tmp/.X11-unix/X${display_number}"
export DISPLAY="$display"

declare -a service_pids=()

fail_if_display_is_running() {
  if xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
    echo "An X server is already responding on display $DISPLAY; refusing to remove its lock or socket." >&2
    exit 1
  fi
}

remove_stale_display_files() {
  if [[ -e "$x_lock" ]]; then
    local lock_pid=""
    read -r lock_pid < "$x_lock" || lock_pid=""

    if [[ "$lock_pid" =~ ^[0-9]+$ ]] && kill -0 "$lock_pid" 2>/dev/null; then
      local process_args
      process_args="$(ps -p "$lock_pid" -o args= 2>/dev/null || true)"
      if [[ "$process_args" == *"Xvfb $DISPLAY "* || "$process_args" == *"Xvfb $DISPLAY" ]]; then
        echo "Xvfb process $lock_pid is alive for display $DISPLAY but is not responding; refusing to remove its lock." >&2
        exit 1
      fi
    fi

    echo "Removing stale X lock for display $DISPLAY."
    rm -f "$x_lock"
  fi

  if [[ -e "$x_socket" ]]; then
    echo "Removing stale X socket for display $DISPLAY."
    rm -f "$x_socket"
  fi
}

clear_stale_browser_profile_locks() {
  local profile_dir="${PLAYWRIGHT_USER_DATA_DIR:-/home/pwuser/profile}"
  local lock_file

  for lock_file in \
    "$profile_dir/SingletonLock" \
    "$profile_dir/SingletonCookie" \
    "$profile_dir/SingletonSocket"; do
    if [[ -e "$lock_file" || -L "$lock_file" ]]; then
      echo "Removing stale Chromium profile lock: $lock_file"
      rm -f "$lock_file"
    fi
  done
}

start_xvfb() {
  Xvfb "$DISPLAY" -screen 0 1920x1080x24 -nolisten tcp -ac &
  service_pids+=("$!")
}

wait_for_xvfb() {
  for _ in $(seq 1 50); do
    if xdpyinfo -display "$DISPLAY" >/dev/null 2>&1; then
      return
    fi
    sleep 0.1
  done

  echo "Xvfb did not start on display $DISPLAY" >&2
  exit 1
}

start_desktop_services() {
  fluxbox >/tmp/fluxbox.log 2>&1 &
  service_pids+=("$!")

  x11vnc -display "$DISPLAY" -forever -shared -nopw -localhost -rfbport "$rfb_port" &
  service_pids+=("$!")

  websockify --web=/usr/share/novnc 6080 "127.0.0.1:${rfb_port}" &
  service_pids+=("$!")
}

stop_child_processes() {
  if ((${#service_pids[@]} == 0)); then
    return
  fi

  kill "${service_pids[@]}" 2>/dev/null || true
  for pid in "${service_pids[@]}"; do
    wait "$pid" 2>/dev/null || true
  done
}

run_collector() {
  dotnet /app/publish/Collector.Playwright.dll
}

main() {
  trap stop_child_processes EXIT
  trap 'exit 0' INT TERM

  fail_if_display_is_running
  remove_stale_display_files
  clear_stale_browser_profile_locks
  start_xvfb
  wait_for_xvfb
  start_desktop_services
  run_collector
}

main "$@"
