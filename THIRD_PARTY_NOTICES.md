# Third-Party Notices

This project does not depend on any third-party code at build or run time (see `CLAUDE.md` — the core library is intentionally dependency-free beyond the .NET BCL). The notice below covers a one-time use of external data during development, not a runtime or build dependency.

## Michael Wayne Arnold's `Date::Lectionary`

The Sunday/Holy Day lectionary data in `src/FiveTalents.Calendar/Resources/sunday-lectionary.json` was cross-checked against an independent transcription of the same BCP 2019 lectionary, embedded as XML data in Michael Wayne Arnold's Perl module `Date::Lectionary` (https://github.com/marmanold/Date-Lectionary), during the audit documented in `docs/audits/2026-07-08-sunday-lectionary-arnold-audit.md` (issue #19). No code or data from that project is redistributed in this repository; this notice is included per the terms of its license.

```
Copyright 2016-2020 MICHAEL WAYNE ARNOLD

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

The same source (`Date-Lectionary-Daily`, same license and author) is expected to be used again for issue #20 (Daily Office data sourcing); this notice will be updated to reference that module too at that time.
