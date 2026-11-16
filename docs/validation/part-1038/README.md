# Part 1038 stack initialization and frame sizes

The general emitter initializes the Y slots that the argument moves will leave unwritten before emitting those moves. When the last clause has distinct variable parameters or wildcards and no guard, clause selection has no remaining path to the function-clause epilogue. Failures from matches inside function bodies retain their separate handling.

In asn1_db:init/2, both images now start with allocate followed by init_yregs. The OTP frame has three slots and the generated frame has five. The generated code still saves the ets:new/2 result and the tuple passed to loop/1 in Y registers. OTP keeps those values in X registers.

The frame analysis compares the first allocate instruction per matched function in all 1021 differing images. Functions are paired by name and arity. The categories count modules with at least one qualifying function, so module totals overlap. The analysis measures ordinary allocate instructions and does not compare the full lifetime of each frame.

| First frame allocation | Modules | Functions |
| --- | ---: | ---: |
| Generated frame has more slots | 1002 | 33945 |
| Generated image has an allocation where OTP has none | 947 | 24411 |
| Same slot count | 272 | 3121 |
| Generated frame has fewer slots | 3 | 4 |
| Generated image has no allocation where OTP has one | 5 | 10 |

The earlier init_yregs versus move pair is absent from the new top 30. The reverse pair now leads at 836 modules, up from 605. In asn1_db:cast/1 OTP allocates zero slots and uses the get/1 BIF in X registers. The generated version allocates two slots and still needs init_yregs. The opcode sequence ranking ignores allocation operands, so it must be read together with the frame analysis.

All counts in the frame analysis are unchanged from Part 1037. The full run admitted and generated 1363 of 1366 modules. It retained 342 Equal, 1021 Different and 3 SourceRejected. Every module retained its previous status, including ELDAPv3.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [asn1_db:init/2 disassembly, OTP followed by generated](asn1-db-init.txt)
- [Frame analysis](frames.txt)
- [Previous frame analysis](frames-baseline.txt)
- [Frame analysis script](rank-frames.eval)
- [Opcode ranking](opcode-differences.txt)
