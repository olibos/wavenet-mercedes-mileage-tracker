# Wavenet Mercedes Mileage Tracker

A small, scheduled job that connects to the Mercedes-Benz API to retrieve the
current mileage of Wavenet's company vehicles **once a month**, and stores the
result for internal fleet reporting.

## Why this exists

Wavenet operates a number of Mercedes-Benz vehicles as part of its company
fleet. Today, mileage is tracked manually (driver self-reporting or physical
odometer checks), which is slow and error-prone. This job automates that by
pulling the odometer reading directly from the official Mercedes-Benz API on
a fixed monthly schedule, giving Wavenet an accurate, low-effort record of
mileage per vehicle over time.

## What it does

1. Authenticates with the Mercedes-Benz API using OAuth2 client credentials.
2. For each registered vehicle (identified by VIN), calls the Vehicle
   Status / Odometer endpoint.
3. Records the mileage reading together with a timestamp.
4. Writes the result to the configured output (CSV, storage table, or similar).
5. Runs automatically once a month via a scheduled trigger — no manual
   intervention required.

## Tech stack

- .NET (C#)
- Mercedes-Benz Connected Vehicle / Fleet API
- Scheduled via an Azure Function Timer Trigger (or a GitHub Actions
  scheduled workflow, as an alternative)
- Configuration via environment variables

## Configuration

| Variable              | Description                                            |
|-----------------------|---------------------------------------------------------|
| `MERCEDES_CLIENT_ID`     | OAuth2 client ID issued by the Mercedes-Benz Developer Console |
| `MERCEDES_CLIENT_SECRET` | OAuth2 client secret                                   |
| `MERCEDES_API_BASE_URL`  | Base URL of the Mercedes-Benz API                      |
| `VEHICLE_VINS`           | Comma-separated list of Wavenet's fleet VINs           |
| `OUTPUT_TARGET`          | Where results are written (storage connection string, file path, etc.) |

## Data usage

Mileage data retrieved by this job is used **exclusively** for Wavenet's
internal fleet administration: maintenance planning, lease mileage-cap
monitoring, and cost allocation. No data is shared with or sold to third
parties, and no data beyond the odometer reading is requested.

## Opt-in, not mandatory

Connecting a vehicle to this tool is **entirely voluntary**. Employees are
never required to link their car or share data through the Mercedes-Benz
API — it is offered purely as a convenience to avoid manual reporting.
Anyone who prefers not to opt in can continue reporting their mileage
manually, exactly as before, with no impact on them.

## Setup

1. Register an application in the Mercedes-Benz Developer Console.
2. Request access to the Vehicle Status / Odometer API scope.
3. Configure the environment variables listed above.
4. Deploy the job and confirm the monthly schedule is active.

## Scope & frequency

This job calls the API **once per vehicle, once per month** — it does not
poll continuously and does not request real-time location or driving data.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.