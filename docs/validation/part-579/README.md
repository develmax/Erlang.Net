# PR109 CI source bindings and module_status review repair

The original PR head0a31e7571 failed Validate CLR implementation run41824014054
before SDK/product execution. The Windows Resource529, focused13 and MFA/native
raw source admissions still named the source bytes from before the code port.
The Linux resource job failed its committed owned506 input admission for the same
reason. Math, Native GC and Linux version651 completed successfully on that head.
Original job IDs and failed local source admissions are retained here.

The repair refreshes only existing source identities, byte counts and dependent
workflow/producer pins. Plan membership, required checks, original OTP pin,
execution/approval flags and compatibility classifications are unchanged; no
workflow logic or trigger is changed. The monitor source freeze gets a successor
with its original parent path/hash preserved. The new immutable receipt directory
is marked as raw bytes across checkouts; existing N1 producer links follow the
corresponding .gitattributes identity. The refresh also maintains the
existing SealedBytes fields, rather than leaving stale informational sizes.
See [scope proof](source-plan-refresh/semantic-scope.json) and the refresh audit.

Review finding4467092326 independently reproduced wrong full error stacks for
module_status(42), module_status([42]) and a list with a preceding valid module.
The managed backend failed all three new cases while retained Erlang passed.
The repair keeps the is_loaded/1 clause at pinned line755, module_status/2 at2094
and every retained list-comprehension frame at2087. Trace construction is bounded
by the configured backtrace depth. Source positions and helper names remain in
their semantic catalogs; the new module logic has no inline string/line literals.

Both backends pass all159 complete original observations and the25-result real
BEAM lifecycle. All17 connected groups pass after the final ordinary build, with
zero warnings/errors and no excluded sources. The source/literal/API audit passes.
Resource529 validates4218 declared dependencies, the exact workflow's13 passive
plans pass, current MFA/native raw bindings pass and all22 CI-reuse controls pass.
Equivalent N1 input-byte closure and the documentation guard pass. These are scoped local receipts, not completed
final-head hosted CI or universal OTP acceptance.

Commands: ordinary dotnet build tests/Erlang.Tests -m:1 --no-restore; exact --case
selections in focused-final.json; tools/validate-code-original.ps1 with the admitted
OTP29.1.1 provider; Resource529 verify-source-plan.py; the source-plan admission
extracted unchanged from validate.yml; verify-mfa-source-bindings.py; unittest
Erlang.CiReuse; check-documentation-layout.cjs; audit-code-source.py. No full local
regression, canonical readiness refresh or manual workflow dispatch is run.

The earlier156-case evidence and token-equivalent literal-refactor proof remain
immutable evidence of their original head. New corpus/provider/source bindings,
reproduction, final159 comparisons and actual hosted CI results have separate
receipts here. Review threads are not resolved; no merge or package publication
is implied by the fix. Hosted final-head checks remain the next required gate.
