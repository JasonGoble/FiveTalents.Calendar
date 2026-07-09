---
name: Bug report
about: Something is producing an incorrect or unexpected result
title: "defect: "
labels: bug
assignees: ''
---

## Expected behavior

What you expected `LiturgicalDay`/the API/the UI to return or show.

## Actual behavior

What it actually returned or showed instead.

## How to reproduce

```
// Date, tradition, and the exact call (e.g. calendar.GetDay(new DateOnly(2026, 4, 5)))
// or the API/UI URL and steps, whatever applies
```

## If this is a calendar/lectionary data question

Please cite the specific BCP 2019 source (document number and page, or a link) that supports the reading/date/rank you expected — this project verifies data-fidelity issues directly against the canonical text, not against other software's output. See `docs/decisions/` for how existing citations were sourced and cross-checked.

## Environment

- .NET SDK version (`dotnet --version`), if applicable:
- Browser, if this is a frontend/UI issue:
