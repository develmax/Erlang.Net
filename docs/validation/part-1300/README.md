# Part 1300 — Sort adjacent stack loads before dynamic apply

Follow beam_block:sort_moves/1 for contiguous Y-to-X argument moves. Sort by the Y source and preserve the move to X0 and the boundaries at constants and the dynamic module result.

The focused test checks the move sequence and execution with ten arguments. The standalone glu comparison increases normalized function matches from 14 to 23 of 30.

- [Focused execution](probe.txt)
- [Focused command](check-dynamic-moves.ps1)
- [glu wrapper comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal, 1,006 Different and zero rejected sources. OTP accepted all differing pairs. Only glu changed and its Code section kept the same length.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
