# Part 1384 — Forward call results through mixed sequences

The shared call-chain emitter now accepts straight sequences that mix variable assignments and calls whose results are discarded. The reader forwards a result to its immediate consumer in X0. It reuses the ordinary call resolver and argument construction scheduler for tuple and list arguments that read both X0 and saved inputs. Stack-slot tracking accounts for all later uses, including inputs read inside the constructors.

The focused fixture exercises ordered state changes, construction using both a saved input and the previous result, and a value that must survive an intervening call. The latter continues through the general emitter. The fixture's five-call sequence matches the OTP instructions after label normalization. The permanent beam_straight_call_probe fixture also matches the complete deterministic OTP 29.1.1 image. Its previous C# image differs.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 994 differing generated images for loading. The corpus and tests use the same compiler assembly.

The change affects 58 images. Seventy-six functions gain normalized matches across 53 modules, with no lost matches. All 31 changed peak frames reach the OTP size across 28 modules. Forty-three images narrow their Code size gap, four widen it and eleven retain their gap. The net gap reduction is 421 bytes. beam_validator has the largest increase at six bytes.

The largest first-difference group drops from 1,844 functions in 567 modules to 1,823 functions in 562 modules. The direct-call assignment subgroup still covers 626 functions in 244 modules. asn1ct:generated_refed_func/1 now matches after label normalization with one saved slot instead of two. asn1ct:compile_proc/3 still needs slot reuse across a constructor binding and a subsequent case selector.

The selected compiler checks, direct whole-module check, new reader layout check and three earlier focused probes passed. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Sequence audit](sequence-prefixes.txt)
- [ASN.1 call and frame instructions](asn1ct-call-frames.txt)
- [Case and comprehension continuation instructions](next-case-frames.txt)
- [Fixture source](straight_call_sequence_probe.erl)
- [Focused oracle check](check-call-sequence.ps1)
- [Focused oracle output](probe.txt)
- [Permanent fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
