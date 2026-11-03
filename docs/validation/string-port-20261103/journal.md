# String port work journal

## 2026-11-03 — Manual string alternative and strict OTP differential validation

**Part status:** partially implemented; full compatibility acceptance remains open.
**Objective and scope:** add a manually implemented C# alternative for all 71
pinned string exports while preserving the original Erlang version. The user's
current request authorizes this alternative; the existing original-only policy
for other pure OTP modules is not changed.

### Changes

- Add the [C# string module](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModule.cs)
  and domain-specific legacy, Unicode, separator, search, trim and lexeme helpers.
  All 71 signatures are present and exercised against actual original exports.
  Registration is explicit. Existing default registration/readiness is unchanged;
  startup-selector integration remains pending.
- Preserve string.erl and string.beam byte-for-byte. Their pinned Git blob and
  official SHA-256 identities are recorded in [string backends](../../string-backends.md).
  Both alternatives execute through the existing MFA API on .NET.
- Keep exact OTP Unicode tables and streaming rules in explicitly loaded original
  unicode/unicode_util dependencies on the C# runtime. Reuse the existing native
  list_to_float primitive and immutable Erlang terms; port integer-prefix parsing
  with BigInteger. Do not substitute culture/BCL Unicode behavior or call original
  string as a fallback.
- Add a [permanent differential runner](../../../tools/Validation/Erlang.StringCompatibility/README.md)
  and [provider-verified entry point](../../../tools/validate-string-original.ps1).
  The common original observer spawns, monitors and joins workers, retains complete
  ETF results/stacks, and independently inspects 13 binary-backing outcomes.
  The validator fails for value, reason, stack or backing differences.
- Correct differences found by original comparisons: mixed list/binary shapes,
  nested/empty and improper tails, binary search without eager UTF-8 validation,
  grapheme-aware prefix endings, empty separators, legacy validation order and
  early returns, Latin-1 mapping versus Unicode mapping, and numeric remainders.
  Limited binary slices use the original backing instead of re-encoding text.

### Decisions and compatibility

Pinned baseline remains OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5.
No new package or production official Erlang VM is required. The official provider
is a development oracle only. Existing Apache-2.0 notices/attribution are preserved;
no owner-local license draft or package publication is included.

Export coverage is **71/71**, not a claim of 100% semantic compatibility. The
5,226-case expanded corpus matches every value and exception class/reason in each
alternative. Full stacks still differ: 1,030 C# cases and 13 original-Erlang-on-.NET
cases in the pre-backing full run. No existing full-MFA classification is promoted.
Broader upstream/Unicode/resource/scheduler/backtrace-depth and platform acceptance
remain required.

### Validation

| Check | Command/report | Actual result |
| --- | --- | --- |
| Original identities | Pinned git blob comparison and admitted release image hashes | Retained Erlang source/image match exactly |
| Managed module/validator build | dotnet build tools/Validation/Erlang.StringCompatibility -m:1 -v:q | Zero warnings/errors |
| Existing test project build | dotnet build tests/Erlang.Tests/Erlang.Tests.csproj --no-restore -m:1 -v:q | Zero warnings/errors; this is compilation, not a full regression run |
| Retained string/float groups | five exact --case selections; artifacts/string-port/retained-tests.json | 5/5 pass; canonical catalog listing contains 9,233 names |
| First corpus | artifacts/string-port/initial-managed-report.json and initial-beam-report.json | C# initially 246/3,406 value/reason differences; original-on-.NET initially 32 missing-native differences; failed reports retained |
| Corrected first corpus | artifacts/string-port/managed-report.json and beam-report.json | Both 3,406/3,406 values/reasons match; direct HOST callers are not full-stack acceptance |
| Expanded common-worker corpus | artifacts/string-port/final-20261103/ | Both 5,226/5,226 values/reasons match; C# 3/1,033 and original 1,020/1,033 complete stacks match; strict validator fails as intended |
| Isolated final implementation smoke | artifacts/string-port/backing-smoke/managed-report.json | Every export exercised, 71/71 returned outcomes and all 13 backing observations match |
| Final frozen composition | artifacts/string-port/frozen-final/; retained final/ ETF/reports in this checkpoint | Both alternatives match all 5,226 values/reasons and 13 backing observations; C# 1,030 and original 13 full-stack differences remain; strict validator exits 1 |
| Scoped source layout | SDK-backed Erlang.Style against an owned-files-only mirror | Zero files need formatting before the final backing addition; unrelated files untouched |

During validation a separate user-authorized integration merge began in the shared
checkout, with conflicts in runtime and documentation files. Those conflicts are
not resolved by this module task. Preserve the successful pre-merge build reports.
The later isolated build uses retained runtime assemblies, unrelated stdlib sources
from 07da777f6 and the current string overlay. Its final build has zero warnings/
errors; the first isolated build's three warnings are retained separately.
An explicit -RunnerDll lets the same provider-checked command validate that frozen
composition without claiming the conflicting combined source builds.

### Handoff

- Complete logical source frames and native/helper error metadata; retain full
  stacks rather than normalizing or removing them from acceptance.
- Integrate the two alternatives into the existing startup selector, then update
  the canonical MFA ledger/readiness from a passing complete regression. Current
  opt-in coverage must not be presented as a default registration increase.
- Close long mixed-input/resource/Unicode/upstream/platform obligations. Passing
  the finite value corpus and export smoke does not close the user's 100% target.
- After the concurrent merge settles, link this journal and the module scope from
  current progress/compatibility/decision/port indexes, rerun combined-source gates,
  and follow the repository's review/CI delivery process. The incoming history already
  owns the numbered changelog volumes, so this checkpoint uses its separate journal.
  No merge or publication
  result is asserted by this entry.
