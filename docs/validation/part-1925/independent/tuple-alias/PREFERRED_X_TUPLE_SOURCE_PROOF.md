# Prefer the live X alias for an immediately following tuple read

Pinned OTP 29.1.1 precodegen for mod_head:do_head/1 retains the original
argument in Y3 and projects the first field from Y3. The later pinned
codegen listing reads that field from X0 and writes the result to X0.
The following fields remain in Y3 because X0 now owns the first result.
beam_ssa_codegen:prefer_xregs provides the relevant copy and clobber rule.
The same source-defined stage precedes assembly, jump and stack trimming.

The C# patch recognizes the adjacent X-to-Y copy and same-Y tuple read.
It rewrites only the read operand to X and retains the Y copy for all later
uses. It does not propagate through a later producer, register overwrite,
call, allocation or branch. No atom, literal, import or label index is
injected. The module bytes continue to be calculated from its source.

The full B and C2 physical censuses independently found 1,004 candidate
sites in 296 modules. Of those, 140 have exactly matching local register
and field geometry in the pinned reference with the expected X source.
The other 864 are uncertain because other lowering already changed the
surrounding code. The diagnostic counted zero aligned native Y negatives
and zero exact-control images with the candidate shape. The source-based
matcher applies independently from the census.

The uncached C2-derived pilot records 4 Equal, 3 Different, 0 Unsupported,
0 SourceRejected in 70.95 seconds. Both exact parity modules from the
previous work and the exact CT/SSL controls remain Equal. In mod_head,
exactly two disassembled source operands changed, in do/1 and do_head/1.
Other register, dispatch and allocation differences keep mod_head
Different.

Pinned ERTS executed eight real mod_head cases on release, earlier and
candidate images. A file-header case reaches do_head using a scratch
file and isolated ETS MIME metadata. Two errors preserve their file/line
and reasons. The first candidate witness used a differently spelled
absolute path and its structured result differed only in that echoed
path. The canonical-path rerun matched the unaltered case set. Eight
separate synthetic tuple results/errors match all three native lanes.
The permanent regression is red before and green after. It covers the
initial X source and later Y sources after overwrite and call. It is
directly validated against the repository's Func<Task> harness contract.

The reformatted source fixture and production patch were strictly replayed
on frozen D (6f11f7f28f1e3d77e8874cc6d5550d6347a356cd00de6160864a59ac2f61299d)
after the verified trace package. No shared workflow or checkout edits
were made. The 300-input whole candidate group runs uncached on one
isolated compiler worker. D-linked verification is queued when that
compile lane releases.

Evidence: pinned-listing, projection-copy-census-c2.terms, pilot.json,
pilot-receipt.json, head-native-reference.terms, head-native-canonical.terms,
fixture-native-receipt.json, focused-receipt.json, d-verified-receipt.json.

### Remaining frame and liveness dependencies in mod_head

The pinned compiler's codegen listing emits the field projection to X1
and its HEAD comparison before allocating one Y slot. Only the HEAD arm
continues to calls. The non-HEAD arm loads the saved record's data and
returns without a frame. The current general checked-entry planner assumes
the first projection wrote a Y slot and models rejected guards that relay
to the function failure. This function writes X0 and the rejected predicate
has a valid return. A later widening requires a dead temporary proof, a
speculative X alias for the checked record, and exclusive frame ownership
through the success branch. The field offset and compared literal both
come from emitted source. This must precede final slot width and typed
operand order. The separate cold-failure proof in unreachable-record-audit
can be evaluated independently.

The private do_head/1 has a guarded record proven at every call site. Its
well-typed input and eventual ordinary call erase the input X register
but retain the Y copy. A record-validation BadRecord still appears after
the final CaseEnd and before the reachable response record failure. Pinned
output omits that block. Follow-up frame work also needs branch-aware tuple
payload extraction and slot ownership across the file-info and error
paths. Those cases remain outside the adjacent source rewrite.

### Raw cohort and Part 1923 replay

The 300-path C2-derived uncached comparison completed in 2,898.09
seconds on one compiler worker. There were 4 Equal, 296 Different,
0 Unsupported and 0 SourceRejected. No whole-file equality changed.
The 296 candidate images changed. The raw report group.json has
SHA-256 D88963593EB9A5667CB6180CCFAE277B9B90A2E043AD45495E464D708F920F23.
The compiler SHA-256 is
15B589B771ED461A2BAEAB8F3CF8213C3E1D1FA62A4A1618CEF5E470CF104B96.
The initial diagnostic normalization ran out of heap after 47 rows.
Its log and original terms stay separate from the 12-image partition
retake. No compilation was reused or rerun for its diagnostics.

Actual frozen Part 1923 archive
87AF6B88D63FD9A632C15548DDAF0B52874F8C3CADB0FF34D8875356C0A0872E
contains the trace patch and its Task test adapters. The E replay moves
only context anchors in the harness. Both production hunks and the
permanent fixture are byte-identical to the D patch. All four intended
paths passed strict whitespace checks and were applied. The E patch
preferred-x-tuple-sources-E-task-harness.patch has SHA-256
5A896AE420DE3572799749136DFB513E43BCC3570C9F6643DCA1499667D245A3.
Separate D and E linked native scopes follow the raw cohort in one
serialized compiler lane.

### Physical D and E witnesses

D's six-image raw scope improved from 3 Equal / 3 Different to
4 Equal / 2 Different due to the trace correction. No raw equality
was lost. The targeted E replay, where trace already ships in the
base, retained 4 Equal / 2 Different without a raw status change.
Its compiler hash is
DB9B63F3F2CE50A9FB0C4C6C217526F33519724D1E09EBDA48B49EBEC6933650.
The build took 93.73 s and the uncached before/after runs took
20.82 s and 25.55 s. All four registered Task tests passed in each
linked build. Eight real mod_head native cases and eleven real xref
cases matched pinned output in both. See d-scope-receipt.json,
d-focused-receipt.json and e-linked-receipt.json.

The first D continuation incorrectly required whole-module xref bytes
to match even though xref has adjacent projection sites. The raw
D before/after reports remained immutable. The retake verified that the
mixed selector in analysis/2 is instruction-identical. The only
disassembly changes in the rest of xref were seven source-register
rewrites, each proven by its actual preceding move, and 55 derived
closure identities. The same exact delta and native case hashes
recurred on E. The original failure remains in d-linked-durable.stderr.log.

### Completed function and loader diagnostics

The initial single VM had an eheap allocation failure after 47 lines.
A retake with the bounded read-only cache evaluator completed
24 batches of 12 rows. The final eight xmerl images still exhausted
heap when grouped in one VM, so each was processed separately.
All original error output and intermediate evidence remains available.
No compiler job was repeated for these diagnostics.

All 296 module pairs are covered. There are 923 changed normalized
function bodies, 17 pinned instruction matches gained, zero lost
and no frame-size deltas. One gain, supervisor's
validAutoShutdownForStrategy/2, was already attributed to trace in its
separate scope. Pinned ERTS prepared all 888 physical reference, before
and after images. The final summary in
group-diagnostics-tail-retake/summary.terms has SHA-256
0598BF00A47C7E448E38383BDF5FF4D914E99302F2F6CC569559DE4D333BA48B.

### Physical Part 1924 replay

The identical four-path E patch strictly applied to frozen Part 1924
source 2853E8BAFFFC6F168B16BEA3373CE1130C9F201CCB5E39A42DA614C21E11AB91.
F's prior compiler has SHA-256
04ED694A9A048CE910A757E2DA9F6F429DC62E791085FDC77B2A2E6C1207AF84
and unchanged runtime 7445A7EA. The linked compiler is
8F82DFFD1047E08AE8021EA8FF2DB239066ADDC4DAD902973BF312EACE6C8D89.
The build took 101.84 s. Eight-module uncached before/after comparisons
took 20.24 s and 19.87 s. Both report 4 Equal, 4 Different, 0
Unsupported and 0 SourceRejected with no equality changes.

Thirteen normalized function bodies changed. httpc_manager:terminate/2
and digraph:delete/1 became exact against the pinned instruction bodies.
No normalized match was lost. All 12 physical before, after and pinned
images prepared in ERTS. Four Task tests passed. All eight mod_head and
11 xref native cases agree with pinned behavior. Real digraph ETS-backed
creation, path, edge, delete and invalid-record results also agree.
The first digraph oracle check had incorrect expected terminal values.
The pinned return is undefined after deletion and a badrecord error for
an invalid graph. That original error and the corrected case results are
both retained.

See f-replay-receipt.json, f-linked-receipt.json and
f-normalized-summary.terms. The independent 296-module evidence
remains available for cumulative Part 1925.

