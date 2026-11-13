# Part 1550 — Capture returned closures from argument registers

When a function directly returns an anonymous closure, the emitter can capture the parent arguments from X registers and return without a stack frame. The general closure emitter still builds the lifted function, with the live count for the heap check calculated from the highest captured argument register.

In xmerl_xsd_type:enumeration_fun/2 the allocation, saved-register move and deallocation are gone. The instruction sequence matches OTP after label and closure identifier normalization. The standalone probe covers two captures separated by an unused argument and a closure with no captures. All four outputs match on the OTP VM. [Disassembly](enumeration_fun.txt) · [Probe](beam_register_captured_closure.erl) · [Observed outputs](fixture.txt).

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. ELDAPv3 remains byte-identical from C# ASN.1 generation. Of the 64 changed images, 54 gained matching functions after normalizing labels and closure identifiers. There are 95 newly matching functions and zero lost matches. OTP accepted all 976 differing images for loading. Three repository regression checks passed. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt) · [Tests](focused.json).
