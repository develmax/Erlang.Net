# Part 1312

Clause planning accepts a literal or flat tuple in a list head. A failed head test leaves the original list available to the next clause. Once the sole nested tuple passes its shape test, the final extracted field can occupy the original list register when the tail call needs the field there.

The focused cases compare complete instructions and behavior against OTP 29.1.1 for literal and tagged tuple heads. They exercise improper lists, failed literal comparisons, wrong tuple tags and wrong arities. The variable tail and proper list argument probes also passed.

- [Focused command](check-literal-head.ps1)
- [Instruction and execution results](probe.txt)
- [Variable tail regression check](variable-tail-regression.txt)
- [List argument regression check](list-argument-regression.txt)
- [Adjacent cons clauses](shared-cons-candidates.txt)

The full run accepted all 1,366 sources and generated all 1,366 release files. It reported 361 Equal and 1,005 Different. Of 65 changed files, 64 narrowed the Code size gap and one retained the same gap. Twelve additional function instruction streams matched after label normalization. OTP accepted all differing images.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Function comparison](normalized-functions.txt)
- [OTP loader](loader.txt)
