# Part 1267 — Match mapped queue callers and closed atom tails

Keep the local Unicode helper result in X0 while computing the original list length from its saved slot. Place the count and converted list in their queue argument registers, then trim the frame before the external call.

Use a closed atom selection for local tails with constant operands. Keep the selected key in X0 when it is the first argument, then share branches that have the same target and remaining operands. Fold a NIF error argument into one literal when its private input is a proven integer.

The standalone `gl` image matches 1,066 of 1,068 functions after label normalization, up from 1,058. The generated Code section falls from 57,097 to 56,862 bytes, with 56,777 in the release. ExpT, LitT and LocT now match byte for byte. The remaining functions are `init_nif/0` and `rec/1`.

The atom sets are identical and the decoded import sequence matches. The first atom ordering difference comes from the order of receive tags in `rec/1`.

For `snmp_standard_mib`, the shared tail dispatch raises the number of functions matching after label normalization from 14 to 44.

The full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Eight images changed. The sum of their absolute Code size differences fell by 705 bytes. Four moved closer by this measure and four moved farther. OTP accepted all 1,007 differing pairs, and ELDAPv3 remained Equal. The corpus gl image matches the standalone rebuild.

- [Normalized SNMP function comparison](snmp-dispatch.txt)
- [Probe source](mapping_queue_probe.erl)
- [Probe command](check-mapping-queue.ps1)
- [Execution result](probe.txt)
- [Normalized gl function comparison](gl-functions.txt)
- [Remaining gl instructions](gl-remaining.txt)
- [Standalone gl sections](gl-chunks.txt)
- [Decoded table comparison](gl-tables.txt)
- [Full corpus](otp-corpus.json)
- [Baseline chunk ranking](baseline-chunk-ranking.txt)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
