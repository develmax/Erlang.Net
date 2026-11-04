# Focused gen_event compatibility

Build this project, then run `tools/validate-gen-event-original.ps1` from the
repository root. The script verifies the admitted OTP 29.1.1 provider, compiles
the identical observer and unchanged upstream fixtures, records provenance and
replays the corpus on C# and original BEAM implementations. No full regression
is launched.

The runner accepts `repository corpus-directory Managed|Beam [case]`. A single
case run is diagnostic and cannot be admitted as complete acceptance. Full
reports require all 105 observations and all 44 non-metadata exports. Actual
backend source owners are checked before execution. All ordinary observations
are compared as complete terms, including error classes/reasons/stacks. The
status scenario explicitly verifies the parent PID relationship.

The ten unchanged upstream functions are add_handler, delete_handler,
swap_handler, swap_sup_handler, notify, sync_notify, call, info, get_state and
replace_state. This is a scoped subset of gen_event_SUITE, not the entire OTP
Common Test suite. `Reference/` retains the upstream Apache-2.0 notices.

Actual original init/code_server boot supplies feature/formatting dependencies;
missing providers are not replaced with successful stubs. The runtime's existing
native storage, file bootstrap and logger integrations remain explicit host
dependencies. Runtime disposal is bounded and failures fail the report.

`[case]` can be combined with `depth=N`; each is optional. Validation names,
contract counts and operation deadlines have separate semantic owner files.
Run `py -3 tools/Validation/audit-gen-event-literals.py .` before publication.
The audit covers production/host logic, retaining small structural arities/indices.
Part584 evidence retains a Managed depth3 upstream zero-timeout race; primary
acceptance passes both backends. Do not infer all-profile or scheduler parity.
