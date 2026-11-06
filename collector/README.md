# Playwright vacancy collector

A .NET 10 app that launches visible Chromium with Playwright, opens the configured URL, and prints the HTTP status, page title, and final URL. Xvfb provides a virtual display; noVNC lets you interact with the browser.

Set `COLLECTOR_TARGET_URL`, `PLAYWRIGHT_DISPLAY_NUMBER`, and `PLAYWRIGHT_NOVNC_PORT` in the root `.env` file, then start from the project root:

```sh
docker compose up --build -d
```

Open `http://localhost:6080/vnc.html?autoconnect=true&resize=scale` to use the browser. Log in to hh.ru once; Chromium's profile is stored in the `playwright-profile` Docker volume and survives container restarts.

Stop the browser with `docker compose down`. The profile volume remains unless explicitly removed.
