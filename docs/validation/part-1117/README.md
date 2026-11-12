# Part 1117 — rank remaining nested calls by entry shape

The scan pairs named functions in the release and generated images, compares their first allocation sizes, and classifies tail expressions in the release abstract code. It inspected all 1,015 differing images. The counts refer to functions whose generated first frame is larger than OTP's. A function or file can appear in several categories. This is a source and frame correlation, so it does not prove that the nested call causes the entire frame difference.

| Tail call shape | Files | Functions |
| --- | ---: | ---: |
| Direct nested calls, simple operands | 511 | 4,607 |
| One child call that contains another call | 106 | 179 |
| One child call with a list operand | 106 | 175 |
| One child call and a list beside it | 82 | 128 |

The second scan splits the largest group by clause entry and prefix.

| Entry shape | Files | Functions |
| --- | ---: | ---: |
| Multiple clauses, variable patterns, guard and direct tail expression | 175 | 312 |
| One unguarded variable clause, preceding expressions | 151 | 2,787 |
| Multiple clauses, cons pattern and direct unguarded tail expression | 113 | 177 |
| Multiple clauses, variable patterns and direct unguarded tail expression | 111 | 211 |

In xmerl_xsd_type:fQuotient/2, OTP tests the input in X registers and emits floor as a gc_bif directly in the argument register before a frameless recursive call. The C# emitter copies the entry values to Y slots and emits floor as an ordinary external call. This isolates a first lowering opportunity within the highest-ranked entry shape. The later arithmetic branches also differ and require separate alignment.

- [Call-shape scan](nested-call-shapes.eval)
- [Call-shape ranking](nested-call-shapes.txt)
- [Entry-shape scan](simple-call-entry-shapes.eval)
- [Entry-shape ranking](simple-call-entry-shapes.txt)
- [Release and generated disassembly](fquotient.txt)
- [Corpus used](../part-1116/otp-corpus.json)
