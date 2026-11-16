# Part 1260 — Validate tagged list heads before frame allocation

When entry clauses share a list head tag and arity, test the extracted head in its X register before allocation. Keep original arguments available on the entry failure edge. A single unguarded clause with distinct variables can extract tuple fields directly from X to their Y slots and avoid saving the whole cons or tuple.

The probe checks single and shared clause entry, direct field extraction, wrong tags and arities, repeated variable matches, and a separate nil fallback.

In `dbg_ieval:eval_zip/5`, the tagged tuple check and the register class used for field extraction now match the release. The generated frame shrinks from 20 slots to 17. OTP uses eight and initializes fewer slots before the first call.

All 1,366 modules generated successfully, with 359 Equal, 1,007 Different and zero rejected sources. All 44 changed images moved closer to the OTP Code length, by 2,372 bytes in aggregate. The frame comparison found 79 smaller initial frames. OTP accepted all 1,007 differing pairs.

- [Probe source](tagged_cons_probe.erl)
- [Probe command](check-tagged-cons.ps1)
- [Execution result](probe.txt)
- [Debugger release entry comparison](release-entry.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Frame delta](frame-delta.txt)
- [OTP loader](loader.txt)
