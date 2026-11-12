# Part 1345 — Tail calls for wx option commands

The wx option emitter accepts a final queue call after mapping the option list. It keeps the mapped list in X0, checks the wx environment in X1 and emits a tail call with a zero-slot frame. The existing command-and-reply form still receives the reply. The missing-environment branch uses the error expression's source location.

The focused comparison matches the normalized tail call instructions with OTP. It verifies mapped arguments, direct return, replies, missing environment and invalid options in OTP with a stub for wxe_util. No native GUI was used.

The full Windows OTP 29.1.1 run generated 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. wxGridCellBoolEditor moved to Equal and all previous Equal images remained equal. The only other changed image was wx_misc. Its Code size gap narrowed by 55 bytes. OTP accepted all 1,001 differing images for loading.

Every remaining different image differs in Code and at least four other chunks. Chunk differences include effects downstream of code generation, such as labels in ExpT and LocT. The first-allocation audit found a larger frame in at least one matched function in 988 modules and an allocation where the release has none in 925 modules. The groups overlap and the audit measures ordinary allocate instructions. A separate wx audit found text conversion and option mapping in 112 clauses across 50 modules, including eight clauses with multiple conversions.

- [Full corpus](otp-corpus.json)
- [Nine-module wx comparison](wx-subset.json)
- [Focused check](check-wx-option-queue.ps1)
- [Focused output](probe.txt)
- [Changed images still differing](changed-different-images.txt)
- [OTP loader](loader.txt)
- [First-allocation audit](frames.txt)
- [Text conversion and option mapping audit](wx-text-option-audit.txt)
