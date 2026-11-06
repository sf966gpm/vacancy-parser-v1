# Vacancy API

The API accepts HH vacancy parse jobs and publishes them to the durable `collector.parse` RabbitMQ queue. The collector remains responsible for opening HH in Chromium and processing each job.

Start the API, RabbitMQ, and collector from the repository root:

```sh
docker compose up --build -d
```

Submit a job to `POST http://localhost:8081/api/hh/jobs`. In development, the OpenAPI document is available at `http://localhost:8081/openapi/v1.json`.

The API currently publishes jobs but does not persist presets, job history, or parsed vacancies. Those features will be added when their storage and result contracts are defined.
