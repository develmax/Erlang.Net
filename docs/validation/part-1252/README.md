# Part 1252 — Fold identity cases before local tail calls

When a terminal atom case contains a nested case that returns its selector unchanged, emit the nested selector as a local tail call. The tuple arm must reconstruct the same value and the fallback must bind a fresh variable. Other supported arms return literals.

The instructions of `test_server_node:start_which_node/1` match Windows OTP 29.1.1 after label normalization. The frame falls from three slots to zero and the generated call-site and case failure lines are no longer needed. The execution probe also checks that an already bound fallback keeps its match constraint.

The corpus generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Only `test_server_node.beam` changed. Its Code length moved 68 bytes closer to OTP. OTP accepted all 1,007 differing pairs.

The same release has direct local tail calls in `inet:getservbyport/2` and `net_adm:longshort/1`. Those cases require argument lifetime and return handling beyond this identity fold.

On the part 1251 baseline, matching atom selector key sets within the same function identified 926 release tail branches absent from the corresponding generated basic blocks across 224 images. This ranks candidates by instruction shape. It does not establish that one transformation applies to all of them.

- [Probe source](terminal_identity_atom_case_probe.erl)
- [Probe command](check-terminal-identity-atom-case.ps1)
- [Probe result](probe.txt)
- [Release disassembly comparison](start-which-node.txt)
- [Local tail branch ranking](atom-tail-ranking.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
