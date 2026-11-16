# Part 1317

Clauses with distinct atom keys in one argument position can share a select_val instruction when their other arguments are variables and their bodies are direct tail calls. The emitter keeps the existing call argument scheduling and sends unmatched terms to the function information label.

The focused probe verifies instruction and behavior parity with OTP 29.1.1 for the first and second argument positions, argument permutations, and function_clause errors. The release eval/3 in xmerl_xpath_lib also matches, including the line for its external call.

- [Focused command](check-atom-tail-dispatch.ps1)
- [Instruction and execution results](probe.txt)
- [XPath release function comparison](xpath-eval.txt)
- [Atom selectors after the first argument](nonfirst-atom-dispatch.txt)
- [Integer selectors in the remaining corpus](integer-tail-dispatch.txt)
- [Typed integer selector and shared target example](integer-dispatch-example.txt)

The final corpus accepted and generated all 1,366 files with 363 Equal and 1,003 Different. The 36 changed images contain 26 additional function instruction streams that match after label normalization, with no lost matches in that comparison. The Code size gap shrank in four images and grew in 32. OTP accepted every differing image.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Function comparison](normalized-functions.txt)
- [OTP loader](loader.txt)
