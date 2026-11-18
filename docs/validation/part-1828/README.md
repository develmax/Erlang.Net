# Part 1828 — Remove list argument frames before local tail calls

The leaf allocator now handles a combined entry stack and heap
allocation and a framed local tail call. Backwards liveness includes
all X arguments consumed by the call. The local callee label has no
control-flow edge inside the caller. A combined allocation becomes a
heap check. Forward analysis still proves that its X root prefix is
initialized on every reachable path. A local tail call drops the
stack operand when the frame is removed. The existing external tail
call and trapping BIF rules are used as before.

Pinned OTP `beam_ssa_codegen:cg_alloc` emits the heap check for a
frameless function. Its `build_call` selects `call_only` when the
return context has no frame. The C# change extends the existing
leaf liveness and root verifier without adding a dependency.

The permanent fixture forwards a list and record fields to a local
callee. It requires four live X roots and a frameless call.
The pinned, prior, and candidate images each passed three ERTS
controls for the normal value, the bad-record error, and sending
a list. Three further controls on
`diameter_callback:handle_answer/5` passed on all three real images.

The representative run compiled 43 modules in 91 seconds. Its seven
Equal and 36 Different statuses were retained, and ERTS admitted all
18 changed images. The full uncached comparison used four isolated
processes, 296 seconds, and all 1,366 release modules. The result is
400 Equal, 966 Different, zero Unsupported, zero SourceRejected and
zero gained or lost byte matches.

ERTS admitted all 276 changed images. The normalized comparison
gained 71 function matches and lost none. Frame removal appears in
691 functions whose same-name and arity OTP counterpart is frameless
and in 19 functions without a matching key. Two same-key reports had
a frame in OTP:

- The printed `snmpm_net_if_mt` closure name refers to a different
  body in the reference. Its equivalent tail wrapper is numbered
  `-fun-1-` by OTP and has no frame.
- Pinned `uri_string:transcode/4` needs a frame over
  `bs_init_writable` in one branch. Both C# revisions use an empty
  binary literal. The 3 public transcode controls, including percent
  encoding and an invalid percent escape, produced identical outcomes
  on all three images.

An earlier isolated pass-order experiment deferred all initial
trimming. It did not reduce the frame in `ex_notebook:do_init/1`
and lost one normalized function match while gaining one. The change
was reverted. Its patch, corpus subset and normalized comparison
are preserved here for diagnosis.

Tuple construction remains the next leaf allocation group. The
near-match `mnesia_frag_hash:convert_old_state/1` uses `put_tuple2`
and must account for every list-encoded input operand. The candidate
for that group needs a separate register and root proof.

[Summary](results.json) · [Corpus](otp-corpus.json) ·
[Changed images](changed-images.json) · [Load](loader.log) ·
[Normalized delta](normalized-delta.terms) · [Frame removals](frame-removals.terms) ·
[Fixture controls](fixture-execution.log) · [URI controls](uri-transcode.log).
