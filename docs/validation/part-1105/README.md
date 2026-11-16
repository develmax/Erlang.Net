# Part 1105 — rank initial Y copies whose last read precedes a call

The scan disassembles all 1,015 differing images from part 1104 and pairs functions by name and arity. It counts only functions whose first allocate is larger than the corresponding reference frame. It inspects copies immediately after allocate, then finds Y operands read before the first ordinary call but absent at and after the call. The copied value can be an incoming argument or a register already populated by a pattern extraction.

| First preparation after entry copies | Files | Functions | Copies |
| --- | ---: | ---: | ---: |
| Register moves only | 628 | 2,911 | 3,848 |
| Test | 545 | 2,719 | 3,631 |
| Try | 184 | 335 | 590 |
| Catch | 95 | 179 | 292 |
| Label | 103 | 150 | 204 |
| Heap check | 63 | 73 | 129 |

Files can occur in more than one group. This is a linear instruction scan and a lower bound on potentially avoidable copies. It treats ambiguous slot references as reads and does not prove control-flow liveness, register interference, or that each copy eliminates a frame slot.

The first moves-only example, xmerl_xsd_type:check_NMTOKEN/1, has a single cons pattern, matches the first call result against true and tail-calls on the saved tail. OTP extracts the head to X0 and the tail to Y0. The generated code stores both in Y and also preserves the incoming list and the first call result. Its frame is four slots compared with one in OTP. This establishes a concrete entry-lowering case within the largest group.

- [Reproducible scan](initial-copy-liveness.eval)
- [Full ranking](initial-copy-liveness.txt)
- [OTP and generated instructions for check_NMTOKEN/1](check-nmtoken.txt)
- [Corpus used](../part-1104/otp-corpus.json)
