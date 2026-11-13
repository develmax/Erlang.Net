# Part 1601 — Emit a direct BIF for simple element calls

The general emitter uses the existing two-operand element instruction for body calls with variable or literal operands when source resolution identifies the Erlang BIF. The failure label is zero and the call location precedes the instruction. The existing known-tuple projection remains available.

In the OTP VM, the fixture returns the same tuple for valid input and raises badarg for an invalid index and a non-tuple. The fixture still differs in register allocation. Three focused managed checks passed. The builds used a larger .NET stack and disabled shared compilation and analyzers. [Fixture](beam_element_body.erl) · [Previous disassembly](baseline-disassembly.txt) · [Current disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The two external element calls in asn1ct:add_once_tobe_refed_func/1 are now direct BIF instructions. C# still uses five slots where OTP uses two. [Release function](asn1ct-disassembly.txt).

All 1,366 pinned release sources were accepted and generated. There were 392 Equal and 974 Different, with ELDAPv3 Equal. Of 220 changed images, 218 moved closer to OTP in Code size and two retained their Code size. The total gap decreased by 7,161 bytes. No function newly matched after label normalization. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt).
