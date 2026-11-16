# Part 1363 — Match tagged call results in registers

The entry call planner checks an ignored tagged payload in X0 before the terminal call. It retains the live input arguments and an earlier call result when the continuation uses them. A tagged projection with no further input constraints emits one tuple test before extracting the payload. The failure branch uses the original value and restores the match line when the preceding instructions changed it.

An object command that checks a call result after mapping options clears the original options slot before the map call. It stores the mapped result in that slot across the checked call and reloads it only for the queue. The successful request trims the frame before queuing and tail-calls the reply handler. A terminal queue call uses the live frame directly.

The focused fixture matches six functions after normalizing labels and closure unique IDs. It checks effect order, named and ignored payloads, wrong tags, wrong arity, non-tuples, missing environment and command arguments with an Erlang queue stub. Six prior probes passed. The two-module subset records wxGLCanvas as Equal. dbg_iserver:safe_call/1 and safe_cast/1 match normalized instructions while their containing image still differs.

The final Windows OTP 29.1.1 run generated all 1,366 images, with 369 Equal, 997 Different and zero SourceRejected. wxGLCanvas is newly Equal and ELDAPv3 remains Equal. OTP accepted all 997 differing images for loading. Across 43 changed images the total Code size gap narrowed by 942 bytes. Fifty-two functions across 33 modules gained normalized instruction matches and none lost a match. Peak stack use reached OTP's count in four functions, including wxGLCanvas:new/2 at two slots instead of nine. Native GUI, full runtime and readiness checks were not run.

The continuation audit finds 110 modules with another call binding directly after a tagged match, including 60 whose next call takes the payload as an argument. A terminal case follows a tagged match in 43 modules.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Two-module subset](subset.json)
- [Debugger comparison](debugger-tagged-match.txt)
- [Focused check](check-tagged-call-match.ps1)
- [Focused output](probe.txt)
- [wx text command regression](wx-text-command.txt)
- [wx option queue regression](wx-option-queue.txt)
- [Private constants regression](private-case-constants.txt)
- [Entry case field regression](entry-case-field.txt)
- [Deferred case field regression](deferred-case-field.txt)
- [Computed case tuple regression](computed-case-tuple.txt)
- [Tagged continuation audit](tagged-continuation-audit.txt)
- [Continuation instructions](continuation-candidates.txt)
- [Audit script](rank-tagged-continuations.eval)
