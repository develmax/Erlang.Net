# Part 1236 — construct tail-call arguments after cons and nil dispatch

Reuse the register-only list extraction and argument construction plans for a two-clause function whose nonempty-list branch builds a tail-call argument and whose empty-list branch makes an ordinary tail call. Preserve the original argument registers on pattern failures. Reuse the consumed list register for the final tuple field after all shape tests succeed.

The execution probe covers an accumulator built from tuple fields, a nested tuple constructed in a list, a local empty-list target, an external empty-list target and failure on invalid inputs. The instructions in sofs:ran/2 match the release after label normalization.

The full release run generated 1,366 images with 357 Equal, 1,009 Different and zero rejected. The Code size gap fell by 243 bytes across inet_db, sofs and xref_utils. OTP accepted all differing pairs.

- [Execution probe](probe.txt)
- [Probe source](cons_nil_calls_probe.erl)
- [Normalized release instructions](sofs-ran-normalized.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
