# Part 1584 — Reuse binary split contexts across continuations

The general body emitter handles a byte-aligned binary split with a variable size directly from its saved input. It keeps the match context in X0, writes both captures to their stack slots and omits the redundant trailing alignment test. The original binary remains available for badmatch. The guarded entry emitter can also forward both captures directly to a local tail call, using the existing argument-move scheduler.

The deterministic 700-byte tail-call fixture matches OTP 29.1.1 for both argument orders. OTP VM execution agrees on five outcomes, including empty tails, negative lengths and short inputs. The reference is OTP 29.1.1 beam_ssa_codegen.erl. [Tail instructions](tail-split.txt) · [Outcomes](outcomes.txt).

Three focused tests passed. All 1,366 sources were accepted and generated, with 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. Two release images changed, ct_netconfc and diameter_dbg. Their Code size differences decreased by 18 and 36 bytes respectively. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Tests](focused.txt) · [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).
