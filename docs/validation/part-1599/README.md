# Part 1599 — Bind fields of constructed tuples directly

A discarded match between an explicit tuple and a same-arity tuple of distinct fresh variables binds its evaluated fields directly. The emitter evaluates operands in their original order before introducing the new bindings. Other patterns retain the existing matching path.

The three-call fixture produces the same term in both OTP VM runs. The C# frame drops from seven slots to three. OTP uses two slots. The instruction comparison shows the removed tuple construction and checks. The release function asn1ct:add_once_tobe_refed_func/1 drops from six slots to five and OTP uses two. [Fixture](beam_constructed_binding.erl) · [Previous disassembly](baseline-disassembly.txt) · [Current disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Release function](asn1ct-disassembly.txt).

All three selected managed checks passed. The successful builds used a larger .NET stack and disabled shared compilation and analyzers. [Test results](focused.txt).

All 1,366 release sources were accepted and generated with the pinned source inputs. There were 392 Equal and 974 Different. ELDAPv3 remains Equal. Eight images changed and their Code sections moved 925 bytes closer to the OTP sizes in total. No function newly matched after label normalization. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt) · [Allocation ranking](allocation-rank.txt).
