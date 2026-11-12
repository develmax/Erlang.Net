# Part 1037 entry list tests

The general emitter checks list shape on incoming registers before allocation when the test is mandatory in every clause. Element matching, alias binding and guard fallthrough remain in their original clauses. Closures and partial forwarding dispatch keep their existing paths.

| First opcode difference | Part 1036 | Part 1037 |
| --- | ---: | ---: |
| OTP nonempty-list test versus generated allocation | 740 | 707 |
| OTP stack initialization versus generated move | 725 | 733 |
| OTP tuple test versus generated allocation | 692 | 692 |
| OTP exact-equality test versus generated allocation | 648 | 646 |

Counts are modules with at least one function exhibiting the given pair. Groups overlap. The complete run built 1363 images and retained all 342 exact matches, including ELDAPv3. All 1021 differing images could be disassembled. Three sources were rejected.

- [Full corpus](otp-corpus.json)
- [Opcode ranking](opcode-differences.txt)
- [Focused checks](focused.json)
