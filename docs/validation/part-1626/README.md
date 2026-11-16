# Part 1626 — Fold constant list suffixes in constructed tail arguments

The register-based tail call emitter folds consecutive constant list cells into a literal when their tail is constant. An improper constant suffix is preserved as an improper list. When the tail is dynamic, the emitter continues to build the required cells at runtime. The existing call-argument construction code and pinned OTP Core lowering provide the reference. No dependency was added.

The permanent fixture covers proper, improper and dynamic list tails. Its full BEAM image now matches OTP 29.1.1 and the OTP VM returned the expected terms on all three calls. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Focused tests](focused.txt).

The full Windows corpus accepted and generated all 1,366 sources. The final counts are 393 Equal and 973 Different. ELDAPv3 remains Equal. The change affected 17 release images and reduced the signed aggregate Code size gap by 148 bytes. Nine more functions match after label normalization, with zero losses. The OTP loader accepted all 973 differing images. In release_handler:extract_rel_file/3 the heap check now matches OTP at 12 words; the order of tuple construction still differs. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt) · [Release function disassembly](release-handler-disassembly.txt).

The installed .NET SDK 10.0.404 was invoked through MSBuild.dll because the checkout pins 10.0.400 with roll-forward disabled. The test and corpus executables ran under .NET 10.0.15. The compiler regression block passed 3,714 of 3,830 tests. All 116 failures reproduced on the part 1625 build, including the runtime port, ETS and source fixtures. The full runtime suite and readiness regeneration were outside this run. [Compiler block](compiler-block.txt).

Frame placement and Y register allocation remain the broadest known Code gap. Three release images in the new ranking have one differing normalized function each. [Frame differences](../part-1623/baseline-first-differences.txt) · [Nearest images](nearest.txt).
