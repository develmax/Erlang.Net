# Part 1293 — Check flat tuple entry patterns before allocating a frame

The baseline scan finds 662 differing modules with a function whose first opcode difference is an OTP tuple test versus a generated allocation. This groups symptoms and includes several forms of tuple matching.

Reuse the planned tuple entry path for single unguarded clauses with a flat tuple pattern. It checks tuple shape and literal fields before frame allocation. The focused probe checks accepted input and function_clause for both wrong shape and wrong literal.

The full corpus generated all 1,366 images with 360 Equal and 1,006 Different. None of the release images changed. On the focused float-literal probe, the new path removes the saved whole tuple and unreachable failure trampoline. The baseline already checked the tuple before allocating.

- [Full corpus](otp-corpus.json)
- [OTP loader](loader.txt)
- [Baseline opcode ranking](baseline-opcode-ranking.txt)
- [Focused execution](probe.txt)
- [Focused command](check-tuple-entry.ps1)
