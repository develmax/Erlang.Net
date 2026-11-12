# Part 1362 — Schedule wx guards and command arguments

Static wx wrappers test text and option types before allocating a frame. The compiler converts one or two text arguments in source order, reuses slots between conversion and option mapping, and keeps the mapped result in a register until queue dispatch. A terminal queue call reuses the current frame, and a request trims expired slots before queueing and tail-calls the reply handler. Missing-environment errors retain the source location of the error expression.

A text-versus-integer overload preserves the scalar in one frame slot. A list-versus-record overload shares the receiver check and reads its class once before dispatching. If an environment reference is already bound, the existing pattern matcher checks its value.

The focused fixture checks four functions against OTP after normalizing labels and closure unique IDs. It executes both images with an Erlang queue stub and checks queue arguments, replies, dispatch, guard failure, invalid options and missing environment, including the bound reference. Six earlier probes passed. No native GUI or full runtime suite was exercised. The 16-module subset has seven Equal and nine Different.

The final Windows OTP 29.1.1 run generated all 1,366 images, with 368 Equal, 998 Different and zero SourceRejected. wxPrintDialog, wxProgressDialog and wxSystemOptions gained byte equality. All previous equal images stayed equal, including ELDAPv3. The three affected functions use two, two and one stack slots respectively, matching the release. Their Code size gaps narrowed by 538 bytes in total. The other generated image bytes are unchanged. OTP accepted all 998 differing images for loading. Readiness regeneration was not run.

The next source audit finds a tagged call-result match followed by more work in 248 of the 998 differing modules. In 57 modules a result from an earlier call is referenced afterward. wxGLCanvas:new/2 shows a reusable slot for the mapped list and a tagged result check that preserves the original value for badmatch.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Sixteen-module subset](wx-subset.json)
- [Focused check](check-wx-text-command.ps1)
- [Focused output](probe.txt)
- [wx option queue regression](wx-option-queue.txt)
- [Private constants regression](private-case-constants.txt)
- [Entry case field regression](entry-case-field.txt)
- [Deferred case field regression](deferred-case-field.txt)
- [Computed case tuple regression](computed-case-tuple.txt)
- [Two-call case regression](two-call-case.txt)
- [Remaining wx instructions](remaining-wx-instructions.txt)
- [Tagged call match audit](tagged-call-match-audit.txt)
- [Audit script](rank-tagged-call-matches.eval)
