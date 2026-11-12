# Part 1098 — reuse expired slots inside terminal clauses and branches

Once a function clause has matched its parameters and guard, its result sequence can reuse dead Y slots independently of other clauses. The general emitter also passes terminal case branch sequences through the same liveness path. Pattern and guard failure edges precede the writes. Intermediate cases that export bindings preserve the previous behavior.

The OTP 29.1.1 probe covers a guarded function clause, a guarded terminal case branch and an intermediate case with an exported binding. The first two frames shrink from four slots to three. OTP uses two. The exported case retains its frame and values. The probe verifies the fallback after a failed guard and the original values observed after the intermediate case.

- [Probe source](branch_liveness_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1097, 753 generated images changed. The Code length gap narrowed in 711 files and widened in none. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)

The oversized-frame group at the first move/init_yregs difference fell from 6591 functions in 813 files to 6579 functions in 812 files. The same-size group remains 27 functions in 16 files.

- [Opcode ranking](opcode-ranking.txt)
- [Frame size split](frame-size.txt)
