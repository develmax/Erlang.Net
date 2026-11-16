# Part 1427 — Lower computed throw reasons through shared call emission

The compiler reuses the local-caller analysis for ordinary external calls and tail expressions in framed functions. It constructs the tuple reason after evaluating its producer and emits raise/3 with an empty trace when the caller proof allows it. The caller analysis preserves throws around after cleanup and conservatively retains the trace when a catch arm consumes it.

In asn1ct_tok:collect_quoted/3 both failure branches now build the reason after lists:reverse/1 and call raise/3. The release still differs in frame trimming, the terminal call form, and other instructions and chunks.

The source fixture compares the call form with OTP and exercises the caught result, an observed stack and a producer failure. Both images pass on OTP. The focused .NET regression invokes the caught and stack-observing paths.

The preceding full report found five or more differing chunk payloads in each of the 984 unequal files. Code differs in all 984. A changed instruction sequence can also reorder the tables it references. The chunk count measures byte differences across the image and cannot identify independent compiler defects.

- [OTP fixture check](check-computed-throw.ps1), [result](probe.txt), [collector instructions](collect-quoted.txt), and [.NET tests](focused.json)
- [Baseline chunk payload counts](baseline-chunk-payloads.txt) and [initial throw audit](initial-throw-delta.txt)

The final corpus generated all 1,366 modules with zero source rejections or emission failures. There are 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading.

The shared change reaches 157 images. The throw and raise call counts now match OTP in 469 of the 530 changed functions and move closer in nine. Forty-nine functions move further away by this count, and 53 emit more raises than OTP. The aggregate Code size gap increases by 4,872 bytes. One additional function gains normalized instruction equality. No whole-image status changes.

- [Corpus](otp-corpus.json), [normalized instructions](normalized-functions.txt), [throw audit](throw-delta.txt), and [throw summary](throw-summary.txt)
- [Loader and Code sizes](loader-and-code.txt), [focused tests](focused.json), and [compiler hashes](compiler-hash.txt)
