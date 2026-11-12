# Part 1198 — distinguish frame allocation from instruction order

The opcode ranking now splits the first reference move versus generated init_yregs mismatch by the preceding allocation operands. Module counts can overlap because a module can contain several affected functions.

| Context | Modules |
| --- | ---: |
| Previous allocate operands differ | 802 |
| Previous allocate operands match | 16 |
| Other preceding instruction or no predecessor | 24 |

In asn1ct:check/2, the reference allocates two stack slots and the generated image allocates twelve. In asn1ct_check:bstring_to_bitstring/1, the counts are one and five. The pinned beam_ssa_codegen.erl computes the Y registers initialized by the subsequent code and omits them from init_yregs. Changing only instruction order would leave the allocation differences intact.

The analyzer ran on all 1,006 differing images from part 1197 and disassembled every pair. The compiler was unchanged in this part. The prior corpus status is 357 Equal, 1,006 Different and 3 SourceRejected.

- [Allocation-aware ranking](ranking.txt)
- [Part 1197 full corpus](../part-1197/otp-corpus.json)
