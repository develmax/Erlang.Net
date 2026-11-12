# Part562: audit of all 71 string APIs

Baseline: **OTP29.1.1**, source commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. Local Windows x64, .NET10.0.400.
Both C# and unchanged original Erlang/BEAM remain separately selectable.

## Source audit and reproduced corrections

[The source catalog](api-source-catalog.json) enumerates all71 signatures,
119 public source clauses, actual clause heads/guards, defaults, the reviewed
contract of each API and SHA256 of its C# handler. The catalog generator checks
the reference byte-for-byte against `git show OTP-29.1.1:lib/stdlib/src/string.erl`;
it does not use the newer checked-out reference HEAD or documentation specs as
an input acceptance policy. Native float parsing is owned by `bif.c` and
Windows `sys_float.c`, not by its Erlang NIF stub.

The independent argument-partition and malformed-input audit reproduced these
previously missing source behaviors:

1. `find` shortcuts only literal `[]`/`<<>>`; nested empty patterns can reach
   a real native `binary:compile_pattern` error. `split` intentionally has a
   different recursively empty-pattern shortcut.
2. Invalid UTF8 pattern conversion returns a structured term which must reach
   `prefix_1`, `split_1` or `bin_search_str` unchanged. Preserve source matching,
   error arguments, native `error_info`, helper PCs and branch evaluation order.
3. `lexemes([], InvalidSeparator)` returns `[]` before checking separators.
4. Binary inverse scanning selects its singleton branch from the original list
   shape, not a deduplicated set. Multiple separators call actual `lists:member`,
   including its errors and caller PCs; binary search uses actual compiled
   resources and `binary:match` instead of bypassing those calls.
5. Mixed binary search performs the original grapheme read before prefix testing.
   Malformed binary continuations can otherwise produce the wrong failure.
6. Restore `cp_prefix`/`cp_prefix_1`: find the consumed prefix using the original
   continuation equality. Eagerly decoding both complete tails wrongly rejects
   valid lazy results and loses `equal_1`/`cp_prefix_1` error frames. The iterative
   implementation retains recursive Erlang frames without CLR recursion.
7. Number conversion performs one Unicode list conversion and the actual native
   or managed list parser. Repeatedly traversing the prefix times out on a
   4096-digit number; the corrected path preserves the untouched remainder.

The permanent fixtures add67 genuine original-OTP cases per backend:66
source-branch regressions and one separate16KiB Jaro resource regression. The old Part561 DLL is an independent negative control: on the64 short
cases it produces24 value/reason differences and6 additional stack differences;
including the two long-number cases reproduces `worker_timeout`. Timeouts are
retained as failed executions, never converted to expected semantic results.
The corrected five permanent groups pass. Bounded16-row actor commands and
separate Jaro groups retain every original10-second worker/command deadline and
15-second generic test deadline; no generic budget is increased. The initial
aggregate-command and whole-test-window failures remain recorded.

The audit also fixes two acquisition defects: external ETF funs are decoded
through the actual runtime function factory, and arbitrary `{bm, Ref}`/`{ac, Ref}`
input tuples are treated as compiled resources only after a real native query
accepts them. Arbitrary input references are still compared exactly. Observer
worker/outcome source positions remain39/42; old frozen fixtures are preserved.

## Validation state

Both final backends pass37145/37145 observations each:20042 normal returned
values (including returned error tuples),17103 exceptions with matching full
stacks, and ten complete13-probe binary-backing profiles. The independent corpora
are26607 argument-partition observations,5045 malformed/Unicode edges,1529 exact
rational rounding-boundary cases and3964 Jaro cases. The [per-API table](api-audit.md)
and [summary](audit-summary.json) bind the current C# assembly SHA256
`1F1450A48C3EDC63E01BC49ACE3D1BFD057DA46D30E381506F02C19A4DB7CFFD`.

The common upstream-inclusive command also passes8522/8522 per backend, with
6364 returned values and2158 complete exception stacks. All44 functional upstream
testcases contribute2112 unique actual exported calls. Depths0,1,2,3,8,32 each
pass23 cases per backend (22 exceptions plus one successful lazy-suffix result).
Five focused groups and both explicit startup/MSBuild consumer examples pass.
The current full regression passes9649/9649 in2100.854 measured harness seconds,
with no OutOfMemory or Stack overflow. All6208 source/input identities are unchanged
during acceptance. SDK style check reports0 files needing formatting; builds
have0 warnings/errors. Readiness admission passes with733 default MFAs and an
explicit Part5619647-test comparison (+2 tests, no default API delta). Historical
Part5619647/9647 is not substituted for the changed implementation.

An additional source review found the C# Jaro candidate loop had quadratic
search cost: different16KiB strings time out on the old DLL but the native OTP
oracle returns0.0. Ordered queues of right-hand positions now reproduce the
original map-of-index-lists algorithm and strict monotone windows. The new
permanent16KiB case passes independently on both backends. At32KiB both selected
backends still exceed the monitored ten-second wrapper's window. A separately
identified **direct-call** probe completes all8/16/32KiB values correctly in both
backends; its measured aggregate times are retained separately (35761ms managed,
51094ms BEAM under the observed run conditions). These are diagnostic timings,
not a controlled performance comparison or a pass of the original ten-second
profile. Neither timeout is hidden or converted into an expected API value.

The deterministic generators are `string_audit_cases.erl`,
`string_audit_edges.erl`, `string_audit_numbers.erl`, `string_audit_jaro.erl`
and `string_audit_regressions.erl`, beside the common
observer under `tools/Validation/Erlang.StringCompatibility`. Each bounded main
chunk and the edge corpus exercises all71 exports. Repeated smoke rows are
counted as observations, not advertised as unique inputs. Source acquisition
uses the admitted genuine original OTP provider; replay uses explicit managed
or original-BEAM selection on .NET with pinned Unicode dependencies.

No original string/Unicode source or image is replaced, no default backend is
switched, and no production execution falls back to an external Erlang VM.
Native floating conversion across every IEEE input/platform, physical ERTS
resource ceilings and exact-head publication/CI are distinct acceptance gates.
Passing finite observations and reviewing every API do not prove universal100%.

## Evidence and commands

- [Full test report](tests.json), [registry](supported-mfas.json),
  [source/input hashes](source-files.json), [assembly hashes](assemblies.json),
  [per-API source clauses](api-source-catalog.json), [per-API outcomes](api-audit.md)
  and [accepted audit summary](audit-summary.json).
- The final raw archive retains every original/actual ETF, compiled observer,
  backend/depth report and upstream result. Retained development archives keep
  old functional/stack/timeouts, the stopped3972-row superseded run, actual
  aggregation failures, direct timing profiles and failed-admission controls.
- `py -3 tools/Validation/audit-string-source.py --reference <pinned-source> --output <new-catalog>`
  checks the source byte-for-byte. Replay each fresh corpus with
  `dotnet <Erlang.StringCompatibility.dll> <repository> <corpus> managed|beam [depth]`.
- `tools/validate-string-original.ps1 -Output <new-output> -RunnerDll <retained-runner> -IncludeUpstreamSuite -DepthProfiles`
  verifies original-provider inputs before/after acquisition and runs both alternatives.
- `summarize-string-audit.py` requires explicit catalog/replay/edges/numbers/jaro/depths,
  current assembly and full-test paths. Old-assembly and partial-report negative
  controls are rejected. It distinguishes the OTP source commit from the dirty
  working-tree identity, and does not promote MFA statuses.

Readiness is regenerated by the actual built `Erlang.Tool.dll readiness docs
<full-report> docs` command. Native `string:list_to_float/1` gains current source
and comparison evidence while retaining Needs verification. The checked local
working tree contains pre-existing map/ordsets changes; this receipt is not a
scoped publication commit or fresh head CI/review. No unrelated changes are
published wholesale, and no public package is released.
