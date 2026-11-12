# Part 1299 — Tail-call after a discarded dynamic apply

When the only remaining action after apply is an ordinary external call with constant operands, trim the dead frame before apply and emit a tail call with zero remaining slots.

The focused test checks the trim, external tail call, guard rejection and retained-binding fallback. The standalone glu comparison increases normalized matches from ten to 14 of 30, including newQuadric/0. The next wrapper differences include contiguous moves sorted by beam_block:sort_moves/1.

- [Focused execution](probe.txt)
- [Focused command](check-dynamic-tail.ps1)
- [glu wrapper comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal, 1,006 Different and zero rejected sources. OTP accepted all differing pairs. Only glu changed and its Code size gap decreased by 11 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
