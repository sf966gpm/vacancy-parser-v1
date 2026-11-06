# Vacancy API

The API stores HH search presets and parse-job history in PostgreSQL. It publishes jobs through a transactional outbox to the durable `collector.parse` RabbitMQ queue. The collector remains responsible for opening HH in Chromium and processing each job.

Start the API, RabbitMQ, and collector from the repository root:

```sh
docker compose up --build -d
```

Create presets with `POST http://localhost:8081/api/hh/presets`, then enqueue a job with `POST http://localhost:8081/api/hh/jobs`. Recent jobs are available from `GET http://localhost:8081/api/hh/jobs`; the OpenAPI document is available at `http://localhost:8081/openapi/v1.json` in development.

The API does not apply migrations during startup. After starting the stack, apply them manually inside the running API container:

```sh
docker compose exec vacancy-api dotnet Vacancy.Api.dll --migrate
```

The collector reports terminal job status to the durable `collector.hh.job-status` queue. Completed job IDs and their status messages are kept in the `collector-state` volume so RabbitMQ redeliveries do not rerun finished browser tasks.

The API also consumes vacancy batches from the durable `collector.hh.vacancies` queue. It upserts records by `(platform, externalId)` and links each vacancy to the parse job that found it. The collector currently only visits search pages, so it does not publish vacancy batches yet.
