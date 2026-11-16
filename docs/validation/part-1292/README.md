# Part 1292 — Assign case and receive labels in emitted block order

Number a deferred boolean case's arms before its successful continuation and failure block. For terminal tuple receives with multiple arity groups, assign the branch and internal failure labels in emitted order, then assign retry and wait.

The standalone C# compilation of gl.erl matches the complete release gl.beam byte for byte. The check reads the reference BEAM only after compiling the source.

The full corpus generated all 1,366 images with 360 Equal, 1,006 Different and zero source rejections. gl is the only module that changed status. ELDAPv3 remains Equal. OTP accepted all differing pairs.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Receive probe](receive-probe.txt)
- [Case probe](case-probe.txt)
- [Baseline opcode ranking](opcode-ranking.txt)
- [Candidates ranked by differing sections](next-candidates.txt)
- [Byte comparison and SHA-256](gl-bytes.txt)
- [Focused command](check-gl.ps1)
