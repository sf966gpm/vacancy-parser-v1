# Playwright vacancy collector

A .NET 10 app that launches visible Chromium with Playwright, opens the configured URL, and prints the HTTP status, page title, and final URL. Xvfb provides a 1920×1080 virtual display; Chromium opens maximized and noVNC lets you interact with it.

Set `PLAYWRIGHT_DISPLAY_NUMBER` and `PLAYWRIGHT_NOVNC_PORT` in the root `.env` file, then start from the project root:

```sh
docker compose up --build -d
```

Open `http://localhost:6080/vnc.html?autoconnect=true&resize=scale` to use the browser. Log in to hh.ru once; Chromium's profile is stored in the `playwright-profile` Docker volume and survives container restarts.

Stop the browser with `docker compose down`. The profile volume remains unless explicitly removed.

## RabbitMQ parse jobs

The HH collector declares and consumes the durable `collector.parse` queue through `Collector.Playwright.Hh.Jobs.HhJobConsumer`. Open the management UI at `http://localhost:15672` and sign in with `RABBITMQ_USER` and `RABBITMQ_PASSWORD` from the root `.env` file. From the `collector.parse` queue page, publish a JSON message like:

```json
{
  "preset": {
    "name": "PHP",
    "query": "PHP"
  },
  "exclusions": {
    "vacancyIds": [123456789],
    "titleCategories": ["SEO", "QA"]
  }
}
```

`preset` carries the display name and query needed by the HH collector. The collector uses the query with the same defaults as hh-parser: area 113, name and description fields, remote schedule, the last three days, and publication-time ordering. `exclusions.vacancyIds` carries vacancy IDs to skip, and `exclusions.titleCategories` carries excluded title categories. Each job opens a new browser tab and closes it when the job finishes. If the browser is closed, the collector reopens it and retries the task once. Vacancy extraction and applying the exclusions will be added next. Failed or invalid jobs are routed to the durable `collector.parse.failed` queue.
