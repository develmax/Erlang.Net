# Record field guards before branch-owned frames

The primary rule is pinned OTP 29.1.1 beam_ssa_pre_codegen:place_frames
and need_frame. The checked tuple and single field predicate are pure.
The continuing branch needs the frame for calls and a saved record.
The rejected branch builds a result directly from that same checked
record.

The census first used 300 independently selected projection inputs.
An input with CRLF was normalized at load time after the first census
selected zero paths. The corrected census found 12 functions in 11
modules. A second physical census of all 1,366 recorded F images found
the same 12 functions and modules, accepting either the original or
validly forwarded tuple source. Five inets do/1 functions have an
aligned pinned prefix and ordinary frameless fallback. Seven sites
have other layout differences.

The C# pass recognizes the checked tuple, verifies the old predicate
result has no consumers, and verifies exclusive ownership of its
rejecting edge. It reserves a fresh X register outside the original
argument set and keeps X0 as the record root. The frame and saved stack
value are placed on the continuing edge. The rejected edge reserves
its result heap with X0 live and then projects the existing field into
the return register. Source file, record tag, field indices, predicate
literal and frame width all follow the generated source. The pass
declines unrelated predicate, stack, handler or fallback shapes.

The initial eight-path comparison under F plus alias retained
2 Equal and 6 Different, with no unsupported or rejected inputs and
no status change. All five inets images changed. xref_base retained its
whole-image digest. The 20.48 s before and 15.81 s after timings are
local observations. All five normalized function bodies changed with
zero whole-function matches gained or lost. All 15 physical before,
after and pinned images prepared in real ERTS.

All five recovered fallback bodies and predicates match their pinned
shapes. The first stricter diagnostic also demanded the same selected
frame width and failed on mod_head. Its continuing frame still has
two slots, while pinned OTP uses one. The complete retake records both
shapes. All other projected guard and normal fallback shape operands
match in mod_dir, mod_get, mod_range and mod_trace. Other parts of
those functions and modules still differ.

The permanent source fixture is red/green and uses the real
repository Func<Task> contract. It covers the recovered frameless
rejected branch and a negative case whose rejected arm calls. The
eight controlled fixture cases preserve native values, reasons and
source lines across the before, after and pinned BEAM images.
Separately, 23 real inets cases agree with the pinned release, covering
normal and malformed terms in mod_get, mod_range, mod_trace and
mod_head. The known file-header case uses only a local scratch file.

The final SDK-formatted four-path patch passes strict replay on
actual frozen F plus alias. Its SHA-256 is
9C30AC5FCA1CFAE213B1B1328184E48000CC89B860F2D6096EA8E16334293A5B.
Scope run binaries precede whitespace-only layout changes. Raw reports
and fixture failures remain separately available.

## Affected group and control check

All 11 candidates from the full physical census and five controls were
compiled uncached on one worker. Both 16-file runs report 4 Equal,
12 Different, 0 Unsupported and 0 SourceRejected. Five affected
inets modules changed. The other six census matches and the controls
retained their previous bytes. There were no new or lost raw matches.
Before took 88.63 s and after took 105.17 s with concurrent machine
load. Raw after SHA-256 E8D3411A10FF5A18D6DC8A0B024A53AECEF407C2B25815364320F601270D9DAA.
The five changed normalized bodies gained or lost no full-function
matches. All 15 before, candidate and pinned ERTS prepares passed.
See group-receipt.json and group-normalized-summary.terms.

## Frozen Part 1925 attachment

The exact frame patch replayed all four intended paths on actual G
archive E03E474D504848BF07D50AEC41B8371BC62A4884CAF55DD56C7CD3C122BC3D92
without source changes. The approved interior record cleanup applied
after it with its hook and Task registration moved to follow the frame
pass. Its body and fixture were unchanged.

Combined compiler SHA-256
1CF5C1FED3A4F444A60FEA4DAA644F7B411AE1CD4202933BF4C971D637DF9476.
Both nine-path uncached runs report 4 Equal, 5 Different, 0
Unsupported and 0 SourceRejected. Only mod_head and mod_trace change
physical bytes. The inherited-line control snmpa_mpd and projection
controls xref_base and digraph retain their exact digests. Raw after
SHA-256 38C4915E45EA7CC71ADCDD06CD2ADCC33E4E81D989203A357E1129CA95DD4F28.
Build 115.50 s, before 24.80 s, after 31.07 s.

Eight mod_head and five mod_trace native cases match the pinned
release. Pinned ERTS prepared the six changed-image triples. All six
linked Func<Task> tests passed. See g-record-frame-replay.json and the
g-combined build, raw, native and fixture receipts.
