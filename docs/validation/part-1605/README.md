# Part 1605 — Construct call arguments containing local function references

The register-based tail call planner now places declared local function references alongside nested tuple and list construction. It reserves closure and term space in a single heap check.

The fixture produces the same return value in both OTP VM runs. Its run/3 instructions match after normalizing the closure identifier. All three selected tests passed. The builds used a larger .NET stack with shared compilation and analyzers disabled. [Fixture](beam_closure_argument.erl) · [Disassembly](run.txt) · [VM output](csharp.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and generated. There were 392 Equal and 974 Different, with ELDAPv3 Equal. Six images changed. Six more functions match after normalizing labels and closure identifiers, including xmerl_xlate:string/3 and four functions in diameter_capx. The Code size gap shrank by 249 bytes. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code sizes](loader-and-code.txt).
