# FiveTalents.Calendar

A .NET library for resolving dates against church liturgical calendars — mapping any date to its liturgical season, observance, feast rank, colour, and assigned lectionary readings.

> **A note on how this was built:** This project is an experiment in *vibe coding* — the practice of building software through a collaborative, conversational workflow with an AI pair programmer (Claude). Every feature, migration, and architectural decision in this codebase was shaped through that dialogue. The goal is not just a working product, but a demonstration that thoughtful AI-assisted development can produce clean, maintainable, production-quality code.

## Supported Traditions

| Tradition | Status |
|-----------|--------|
| ACNA Book of Common Prayer 2019 | In progress — calendar engine and Sunday/Holy Day lectionary complete |
| Revised Common Lectionary | Planned |
| Common Lectionary (1983) | Planned |
| Episcopal Church (TEC) | Planned |

## Projects

| Project | Description |
|---------|-------------|
| `src/FiveTalents.Calendar` | Core library — publishable as a NuGet package |
| `src/FiveTalents.Calendar.Api` | ASP.NET Core Web API host |
| `tests/FiveTalents.Calendar.Tests.Unit` | Unit tests |
| `tests/FiveTalents.Calendar.Tests.Api` | API integration tests |
| `web/five-talents-calendar-web` | Angular 21 companion website |

## Running Locally

```bash
# API (http://localhost:5290)
dotnet run --project src/FiveTalents.Calendar.Api

# Frontend (http://localhost:4200)
cd web/five-talents-calendar-web && npm start
```

## Tech Stack

- **.NET 10** — class library + ASP.NET Core Web API
- **Angular 21** — standalone components, Angular Material, signals-first state
- **xUnit** — unit tests

## NuGet

The `FiveTalents.Calendar` library is designed to be consumed independently of the hosted API. NuGet publication is planned once the ACNA BCP 2019 tradition is fully implemented.

## License

MIT
