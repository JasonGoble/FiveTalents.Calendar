#!/usr/bin/env python3
"""
Flags calendar dates in a date-keyed Daily/Sunday lectionary JSON where a lesson
citation's book breaks continuity with the surrounding days for the same
citation "slot" (type) - a cheap way to spot Holy-Day-proper insertions.

Used originally to confirm the ACNA BCP 2019 Daily Office Lectionary's fixed
Holy Day propers are already correctly embedded in
acna-bcp2019-daily-office-lessons.json (see the closing comment on
FiveTalents.Calendar#46, 2026-09-05) - kept here per that issue's decision to
retain one-time sourcing/verification scripts instead of discarding them.

Usage:
    python scripts/daily-office-continuity-check.py <lessons.json> [MM-DD ...]

With no MM-DD arguments, scans every date in the file and prints only the
dates where at least one lesson slot is flagged. With one or more MM-DD
arguments, prints full context (prev/cur/next) for just those dates
regardless of the heuristic's verdict - useful when you already suspect a
Holy Day and want to see its neighbors even if nothing gets flagged.

Limitations (see #46's closing comment for the concrete cases these missed):
- Compares only the citation's leading book name, so a proper reading drawn
  from the SAME book as the surrounding continuous reading (e.g. a Gospel
  proper on a date that otherwise reads through that same Gospel) is not
  flagged.
- A proper reading whose neighboring days happen to sit at a natural book
  transition anyway is not flagged, since "same book before and after" is the
  signal this script looks for.

Treat its output as a lead worth checking against the source document, not a
final answer.
"""
import argparse
import json
import re
from datetime import date, timedelta

LESSON_TYPES = ["MorningFirstLesson", "MorningSecondLesson", "EveningFirstLesson", "EveningSecondLesson"]


def book_of(citation):
    match = re.match(r"^\s*((?:\d\s+)?[A-Za-z.]+)", citation)
    return match.group(1) if match else citation


def mmdd_offset(mmdd, delta):
    month, day = map(int, mmdd.split("-"))
    d = date(2023, month, day) + timedelta(days=delta)  # non-leap reference year
    return f"{d.month:02d}-{d.day:02d}"


def citations_for(entries, mmdd):
    entry = entries.get(mmdd)
    if entry is None:
        return {}
    return {item["type"]: item["citation"] for item in entry}


def check_date(entries, mmdd):
    prev = citations_for(entries, mmdd_offset(mmdd, -1))
    cur = citations_for(entries, mmdd)
    nxt = citations_for(entries, mmdd_offset(mmdd, 1))
    rows = []
    for lesson_type in LESSON_TYPES:
        p, c, n = prev.get(lesson_type), cur.get(lesson_type), nxt.get(lesson_type)
        flagged = (
            p is not None and n is not None and c is not None
            and book_of(p) == book_of(n) and book_of(p) != book_of(c)
        )
        rows.append((lesson_type, p, c, n, flagged))
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("json_path", help="Date-keyed (MM-DD) lessons JSON, entries as lists of {type, citation}")
    parser.add_argument("dates", nargs="*", help="Specific MM-DD dates to inspect; omit to scan every date in the file")
    args = parser.parse_args()

    with open(args.json_path, encoding="utf-8") as f:
        entries = json.load(f)
    entries = {k: v for k, v in entries.items() if not k.startswith("_")}

    dates = args.dates if args.dates else sorted(entries.keys())

    for mmdd in dates:
        rows = check_date(entries, mmdd)
        if args.dates or any(flagged for *_, flagged in rows):
            print(f"=== {mmdd} ===")
            for lesson_type, p, c, n, flagged in rows:
                marker = "  <-- possible proper (breaks continuity)" if flagged else ""
                print(f"  {lesson_type:20s} prev={p!r:30s} cur={c!r:30s} next={n!r:30s}{marker}")
            print()


if __name__ == "__main__":
    main()
