# Part 1036 corpus opcode ranking

Set `ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES=1` and run Erlang.BeamCorpus with a fresh report path. The tool saves each different generated image under the sibling `<report>.generated` directory, preserving the relative paths of the release images.

Pass the reference installation and generated image directory to `rank-opcode-differences.eval` using `erl -noshell -eval` and `-extra`. The script compares opcode sequences per function after dropping labels, line markers and function headers. It counts a module once per first-difference pair and includes sample function names. Groups can overlap. Operand differences and branch destinations are outside this ranking.

| First difference in a function | Modules |
| --- | ---: |
| OTP nonempty-list test versus generated allocation | 740 |
| OTP stack initialization versus generated move | 725 |
| OTP tuple test versus generated allocation | 692 |
| OTP exact-equality test versus generated allocation | 648 |

All 1021 different images were disassembled. The full corpus has 342 Equal, 1021 Different and three SourceRejected. All statuses remain unchanged, including the exact ELDAPv3 image.

- [Full corpus](otp-corpus.json)
- [Opcode ranking](opcode-differences.txt)
