# Part 1296 — Forward the dynamic module from its producer to apply

Fold a fresh leading module binding into the next dynamic call when the function name and arguments are independent operands and the rest of the function does not read the binding. Reuse the dynamic call emitter that forwards the producer result in X registers.

The focused probe checks the dynamic call and the fallback that retains a later-read binding. In glu, lookAt/9 and newQuadric/0 now use the same frame sizes as OTP. The normalized match count is still two of 30 functions. Register moves, trimming and return lowering still differ.

- [Focused execution](probe.txt)
- [Focused command](check-dynamic-module.ps1)
- [glu instruction comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal and 1,006 Different. OTP accepted all differing pairs. The change affected glu and net_adm. The absolute Code size gap increased by 265 bytes for glu and decreased by seven for net_adm.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
