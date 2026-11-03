# string differential validation

Run from the repository root:

```powershell
pwsh -NoProfile -File tools/validate-string-original.ps1 -Output artifacts/string-check-unique -IncludeUpstreamSuite -DepthProfiles
```

The admitted official OTP29.1.1 provider is a development oracle. It is not a
production dependency. The command refuses existing output directories, verifies
the provider manifest and execution inputs before/after use, compiles the common
observer and executes both explicit startup selections.

Reports retain the baseline, all71 declared/exercised exports, backend receipts,
assembly identities/digests, corpus digest, complete values/errors/stacks and
binary backing. Any value, reason, stack or backing difference fails the command.
`RawStacksMatched` distinguishes literal equality from `BoundResourceStacksMatched`.
The latter requires live binary-pattern queries and a bijection, and has negative
controls; no stack frame or arbitrary reference is filtered out.

`-IncludeUpstreamSuite` executes44 functional testcase functions from the unchanged
pinned `Reference/string_SUITE.erl`, then replays its2112 unique actual string
calls through the same observer. It excludes performance and doctest infrastructure.
`-DepthProfiles` independently checks depths0,1,2,3,8,32 through the real
`erlang:system_flag/2` entry. Source, original ETF and actual ETF are retained.

`-RunnerDll <path>` uses an explicitly retained validator without rebuilding.
Its hash is recorded. The executable's optional fourth argument selects a
backtrace depth for an already acquired depth corpus. See
[current scope](../../../docs/string-backends.md).

## Independent all71-API audit

`string_audit_cases:write(Output)` acquires seven bounded original-OTP chunks.
Each includes the same71 export seeds plus deterministic single/pair argument
partitions, malformed chardata, Unicode boundaries, identities and large indexes.
`string_audit_edges:write(Output)` adds every single-byte input, broken UTF8,
conversion error tuples, lazy mixed continuations and long numeric prefixes.
Compile both at their repository paths using the admitted original provider,
load the common observer, and copy its compiled image into every output chunk.
Replay each with this executable and each of `managed`/`beam`; a complete
successful report is required, including exact errors/stacks and binary backing.
`ApiCoverage` gives the actual expected value/error partitions per signature.

`string_audit_regressions:write(Output)` writes the66 immutable permanent rows.
The runtime test groups replay them in addition to the old97-row fixture; the
old source/image/oracle files remain unchanged. The expanded depth generator
includes22 actual errors and one lazy malformed-suffix value at each of0,1,2,3,8,32.

`tools/Validation/audit-string-source.py` catalogs the71 contracts/119 public
clauses and checks its input against the pinned git source bytes.
`tools/Validation/summarize-string-audit.py` requires all seven successful chunks,
the edge corpus and every expanded depth report for both backends. It rejects
stale handler hashes, mixed assembly identities, missing exports, differences
and partial/listed/failed full-test reports. It produces the per-API review table
and machine-readable evidence index. Counts include repeated seeds; they do not
promote compatibility classifications. See [current all71 audit](../../../docs/validation/string-otp-merge-20261104/final-api-audit.md).
