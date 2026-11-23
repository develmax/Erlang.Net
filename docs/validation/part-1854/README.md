# Part 1854 — Remove dead binary binding tuples and recover range facts

The early source pass replaces a discarded outer tuple binding with
the original inner binary match. It proves freshness, counts all
visible reads and retains the match subject, failure term and source
location. A returned tuple, an existing binding and an observable
inner alias keep their prior lowering. Pinned OTP Core tuple expansion
provides the independent reference. The C# compiler reuses its
existing clause variable traversal and bit-pattern construction.

The success-path binary type pass follows fixed-width integer and UTF
extract producers. Its register facts describe one fallthrough block,
retain existing operand annotations and clear stale locations at
unmodeled operations. Existing switches can introduce a new exact
comparison after the pinned SSA type pass. A packed comparison
between operations on the same match context preserves its existing
typing. The new safe external BIF source-line proof uses the pinned
safe-call classification and a separately verified next line or
safe exit. Calls with potentially reused exception locations retain
their current line marker.

The first linked focused run passes 36 of 37 cases. It adds an
annotation to the established exact BER fixture. Its raw failure,
disassembly and pinned SSA, pre-codegen and codegen listings are
preserved. The earlier SSA dump contains a switch on the extracted
field. Pinned ssa_opt_sw creates an exact comparison after the
type pass. The corrected run reports 37 of 37 passing cases.
The wrong fixture bytes are retained only as diagnostic evidence.

The one-worker 22-module release selection completes in 132.24
seconds at BelowNormal priority while the previous exact runtime
gate runs. It reports 9 Equal, 13 Different, 0 Unsupported and 0
SourceRejected. No whole-file matches change. All ten changed
files prepare in ERTS. All 453 changed normalized bodies retain their
prior equality status. The ssh_channel_sup Line section becomes
exact, with no lost sections.

| Pinned release module | Tuple allocations removed | Element loads removed | Arity checks removed | Outer badmatches removed |
| --- | ---: | ---: | ---: | ---: |
| MEGACO PER v1 | 91 | 182 | 91 | 91 |
| MEGACO PER v2 | 116 | 232 | 116 | 116 |
| MEGACO PER v3 | 122 | 244 | 122 | 122 |
| Total | 329 | 658 | 329 | 329 |

All three binary match instruction counts remain stable. Qualified
call fixture behavior, the two SSH roles and four response forms
match the pinned outputs. PER v1 preserves its six previous
observable outcomes. Five agree completely with OTP. The old
case_clause stack location remains at line 1849, while the reference
reports 1843. Values, error reasons and the binary badmatch subject
agree. No public runtime MFA or third-party package was added.

[Corrected source identity](source-freeze.json) ·
[Initial failure](initial-focused.log) · [Pinned SSA switch](ber-original.ssa) ·
[Corrected tests](focused.json) · [Pilot](pilot.json) · [Time](pilot-receipt.json) ·
[Native loader](pilot-loader.terms) · [Function delta](pilot-summary.terms) ·
[Exact sections](pilot-section-delta.json) · [PER operation counts](per-operations.json) ·
[PER before](megaco_per_media_gateway_control_v1-1.log) ·
[PER after](megaco_per_media_gateway_control_v1-2.log) ·
[SSH outcomes](ssh_channel_sup-2.log).
