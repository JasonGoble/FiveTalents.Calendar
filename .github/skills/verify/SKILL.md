---
name: verify
description: Build/launch/drive recipe for verifying FiveTalents.Calendar changes end-to-end — backend (library/API) and frontend (Angular) surfaces.
---

# Verifying FiveTalents.Calendar changes

Two runtime surfaces, pick based on what changed. Full-stack UI features need both running together.

## Backend surface (library / API)

```bash
dotnet run --project src/FiveTalents.Calendar.Api --urls http://localhost:5299
```

Drive it with `curl`, not by importing the library and calling methods directly:

```bash
curl -s http://localhost:5299/calendar/AcnaBcp2019/day/2026-02-18 | python3 -m json.tool
```

Pick dates that actually exercise the change — e.g. an odd vs. even calendar year for anything touching the Daily Office Year I/II split, a known movable Holy Day (Ash Wednesday = Easter − 46; compute via `GET /calendar/AcnaBcp2019/easter/{year}` first if unsure) for the movable-Holy-Day override, day 31 of a month for the Psalter fallback.

## Frontend surface (Angular day/week view)

Playwright isn't a project dependency (no `.spec.ts` convention exists here, and CI only runs `npm run build`, not `ng test`) — set it up in an isolated scratchpad dir, not the project's `node_modules`:

```bash
mkdir -p /tmp/verify && cd /tmp/verify
npm init -y && npm install playwright --no-save
npx playwright install chromium   # NOT --with-deps, that needs sudo and fails in-sandbox
```

Launch both servers in the background, then wait for them (both come up in ~1-2s locally):

```bash
(dotnet run --project src/FiveTalents.Calendar.Api --urls http://localhost:5299 > /tmp/api.log 2>&1 &)
cd web/five-talents-calendar-web && (npm start > /tmp/ng.log 2>&1 &)
# poll curl http://localhost:5299/... and http://localhost:4200 until both 200
```

Day view URL takes `date`/`tradition` as query params directly (no need to click through the UI to set them): `http://localhost:4200/day?date=2026-02-18&tradition=AcnaBcp2019`.

Minimal Playwright driver — `chromium.launch()`, `page.goto(url, { waitUntil: 'networkidle' })`, then either `page.locator(...).allTextContents()` for structured checks or `page.screenshot({ path, fullPage: true })` for a visual read. Useful selectors already in the day view: `.readings-card` (Eucharist), `.daily-office-card`, `.no-readings-card`, `.readings-table .reading-type` / `.reading-citation`, `mat-panel-title`. The prev/next-day buttons are `button[matTooltip="Previous day"]` / `button[matTooltip="Next day"]` — click these (not just re-navigating the URL) at least once per session to confirm the page's `computed()` signals actually react to client-side navigation, not just fresh loads.

**Cleanup — `pkill -f` alone is unreliable in this sandbox** (has left both `ng serve` and `dotnet run` processes running silently before, and once served visibly stale API responses because of it — see below). Confirm each port is actually dead after killing:

```bash
pkill -f "dotnet.*FiveTalents.Calendar.Api"
pkill -f "ng serve"
sleep 1
curl -s -o /dev/null -w "%{http_code}" http://localhost:5299/... --max-time 2   # want 000
curl -s -o /dev/null -w "%{http_code}" http://localhost:4200 --max-time 2       # want 000
# if still alive: ss -tlnp | grep <port>  (lsof -i:PORT has not reliably shown these
# processes in this sandbox), find the PID from that, kill -9 <pid> directly
```

**`dotnet run` spawns a child process that survives killing the parent.** `dotnet run --project X` forks the actual compiled binary (`bin/Debug/net10.0/<AssemblyName>`) as a child; `kill`-ing the `dotnet run` wrapper PID leaves that child listening and serving the *old* build. Symptom: you rebuild, restart, curl again, and get identical output to before your change — looks like the change didn't take effect, but it's actually a stale server. `ss -tlnp | grep <port>` shows the true PID (the binary path, not `dotnet run ...`) — kill that one specifically, not just whatever `ps aux | grep dotnet` first shows.

## Gotchas

- Ordinary Time weekdays are *not* a reliable "no Eucharist readings" test case — `AcnaSundayLectionary`'s `OrdinaryTimeKey` gives every weekday in Ordinary Time its governing Sunday's Proper readings, not just Sundays. For a genuinely empty `readings` case, use a date in the "Days after Epiphany" gap (`WeekNumber == 0`, between Jan 6 and the first Sunday after) — e.g. `2025-01-08`.
- String enums serialize as PascalCase strings (`JsonStringEnumConverter`) — when asserting on raw API JSON, match e.g. `"FirstLesson"` not `"firstLesson"`.
