# Part 1316

An exact match between equal atom or integer literals is replaced by its literal value during emission. The argument planner sees the folded operand before preparing registers. A discarded match can skip evaluation, and a constant-returning clause can emit its final value directly. Nonmatching literals still raise badmatch.

The focused probe compares the complete instruction streams for a tail call with folded operands and a clause that discards a folded match. It verifies the returned values and the error payload for unequal literals against OTP 29.1.1.

- [Focused command](check-literal-match.ps1)
- [Instruction and execution results](probe.txt)
- [Atom selector tail-call audit](atom-tail-dispatch.txt)
- [XPath opcode comparison](near-equal-xpath.txt)

The final corpus accepted and compiled all 1,366 sources. There were 363 Equal and 1,003 Different. The only twelve changed images all narrowed their Code size gaps. The opcode counts show 240 removed badmatch instructions across eleven SSL modules and ten in zip. OTP accepted every differing image.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Badmatch counts in OTP, part 1315 and part 1316 order](badmatch-counts.txt)
- [OTP loader](loader.txt)
