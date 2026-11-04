# PR108 Unicode review and CI repairs

The published head450d86a75 failed before SDK/product execution in Validate CLR
implementation and Focused Linux NIF version651 contracts. Resource529, the
13-plan passive admission and current MFA/native source bindings retained hashes
from before the Unicode runtime integration. The N1 Unix manifest also retained
the previous .gitattributes bytes. These are source identity failures, not evidence
of a product comparison failure. Original failures are retained here.

The follow-up refreshes only existing declared source identities, byte lengths
and dependent workflow/plan pins. It preserves membership, OTP pin, statuses,
execution approval flags and whole-contract classifications. The Monitor source
freeze has a successor with its old path/hash recorded; the historical snapshot
and all earlier evidence remain unchanged. The refresh audit records each change.
Workflow logic, gates and triggers are unchanged. The Git whitespace attribute
recognizes CRLF in already raw-byte-bound workflows without changing their bytes.

The documentation guard separately used four superseded String separator-source
hashes inherited from main136d956c7. It now validates those current source files
against the later accepted String source/assembly freeze. Historical separator
archive checks remain. A corrupted StringTrim.cs is rejected and its original
bytes are restored; the negative control is retained.

Review discussion4462862795 identified quadratic prefix copies in
UnicodeConversion.Commit. Capacity is now advanced at the same OTP semantic
segment boundaries, while the final writable binary is materialized once.
This preserves source values and referenced backing sizes, including a later
guest binary append. The permanent growth test uses 16 genuine OTP29.1.1 rows:
2k/10k/20k/100k integers, UTF16 big/little, UTF32 and Latin1. Provider identities
are checked before and after oracle capture in growth-provenance.json. The
10k-to-20k allocation assertion rejects the old published Stdlib DLL and passes
the corrected implementation. Runtime/observer allocations are included; this
is a scaling regression control, not a comparative speed benchmark.

Focused validation builds the test/validator projects and selects 20 exact cases
from the harness: Unicode, String dependencies, original function storage,
writable append/receive and opaque callback boundaries. Each backend separately
replays 490 permanent contracts, 711 upstream-suite observations and the related
408 long/nested/error corpus, with 29 backing probes. See the retained reports
for actual outcomes and assembly hashes. Full local regression, global readiness
regeneration and manual workflow dispatch are not run under the user's scope.

Resource529, the13-plan admission, MFA/native raw source binding and documentation
guard are rerun locally. N1 byte checks are replayed separately on Windows;
the actual Linux-only verifiers remain a final-head CI requirement. An attempted
unchanged Linux-verifier invocation on Windows correctly refuses its platform;
that is not counted as a Linux pass. The old-head Math and Native GC workflows
passed; their status is not final-head acceptance for this follow-up.

No new package or license choice is introduced. Reuse the existing
BinaryBackingParameters growth policy and writable BitString implementation;
preserve upstream Apache-2.0 notices. No whole Unicode/module/platform contract
promotion, merge, review-thread resolution or package publication is implied.

Commands: `dotnet build tests/Erlang.Tests --no-restore -m:1`;
`dotnet build tools/Validation/Erlang.UnicodeCompatibility --no-restore -m:1`;
test harness `--case` selections recorded in focused20.json; Unicode validator
`<repository> <corpus-directory> Managed|Beam contracts.etf|upstream-suite-original.etf|extra.etf`;
`py -B .../verify-source-plan.py --plan-sha256 <current-hash> --output <fresh-artifact>`;
`py -B tools/Build/Erlang.Tool/verify-mfa-source-bindings.py --repository . --output <fresh-artifact>`;
the existing validate.yml13-plan inline Python admission and nif-version651-linux.yml
input checks; `node tools/Validation/check-documentation-layout.cjs`.

Next gate: push this follow-up to PR108, inspect checks on its exact final head,
and retain any new failures without an unchanged retry. PR metadata stays English.
