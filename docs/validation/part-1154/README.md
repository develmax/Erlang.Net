# Part 1154 — build list arguments without a stack frame

The baseline audit found 489 modules where a reference function contains a list construction and a frameless tail call while the generated function has a frame. The count includes control flow and does not prove that the list supplies the call.

The shared constructor planner now emits list cells and nested tuples in X registers for a single constructed tail argument. It retains reads of inputs used by other arguments. The regression uses an improper list with a nested tuple and a value used again as a separate argument. Its instructions match OTP after local label normalization and both images return the expected term.

The full corpus generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Code size gaps narrowed in 104 modules. Instructions in 153 functions from 86 modules gained matches, with none lost. OTP prepared all differing images for loading. The three focused tests passed.

- [Constructor audit](part1154-constructor-audit.txt)
- [Audit program](part1154-constructor-audit.eval)
- [Probe disassembly and execution](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
