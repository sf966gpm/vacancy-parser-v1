# Collector Backend

ASP.NET Core 10 API using controllers. The `GET /api/hello` endpoint returns `hello world` as plain text.

## Run with Docker Compose

From the project root:

```sh
docker compose up --build
```

Configure the host port and ASP.NET Core environment in the root `.env` file:

```dotenv
COLLECTOR_BACKEND_PORT=8080
COLLECTOR_BACKEND_ENVIRONMENT=Development
```

The API listens on `http://localhost:${COLLECTOR_BACKEND_PORT}`. OpenAPI is available at `http://localhost:${COLLECTOR_BACKEND_PORT}/openapi/v1.json` in the Development environment.
