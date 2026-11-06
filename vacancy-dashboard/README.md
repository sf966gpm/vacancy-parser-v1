# Vacancy Dashboard

React and TypeScript dashboard for managing HH search presets and parse jobs.

## Run the development container

Start the API stack, then start the dashboard:

```sh
docker compose up --build -d vacancy-dashboard
```

Open <http://localhost:5173>. Compose builds the `vacancy-dashboard:dev` image and bind-mounts this directory into the container, so source changes on the host are picked up by Vite. npm installs dependencies into this mounted directory, so `node_modules` and `package-lock.json` are visible on the host too. The container runs as the `node` user. Set `VACANCY_DASHBOARD_PORT` in the root `.env` if port `5173` is already in use, and set `VACANCY_DASHBOARD_PROXY` to the proxy URL used for npm downloads.

Apply the API database migrations as described in [vacancy-api/README.md](../vacancy-api/README.md) before using the dashboard against a new database.

## Run Vite directly on the host

Start the API stack and apply its database migrations first, then run:

```sh
npm install
npm run dev
```

Open <http://localhost:5173>. Vite proxies API requests to `http://localhost:8081`.
