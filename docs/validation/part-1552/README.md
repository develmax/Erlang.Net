# Part 1552 — Resolve process flag calls in constant call sequences

The constant call sequence emitter resolves imports and returning process_flag BIFs before emitting any instructions. It preserves local calls when a function with that name and arity exists. In logger_handler_watcher:init/1 the nil-pattern check now leads to a frame with no saved arguments, and the resulting instruction sequence matches OTP.

The standalone probe compares a qualified BIF call, a local function with the same signature, and a failed nil-pattern match. Both builds return the same values and produce the same process state on OTP. Three repository regression checks passed. [Disassembly](init.txt) · [Probe](beam_constant_process_flag.erl) · [Observed outputs](fixture.txt) · [Tests](focused.json).

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. Six images changed, each gaining a matching init/1 after label normalization. No matching functions were lost. ELDAPv3 remains byte-identical from C# ASN.1 generation. OTP accepted all 976 differing images for loading. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt) · [First differences](first-difference-ranking.txt).
