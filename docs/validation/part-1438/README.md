# Part 1438 — Branch on short-circuit guard tests

The general guard emitter branches directly on the left operand of orelse when it is a resolved unary type test over a variable or literal, or a short-circuit expression composed of such tests. Those operands can only return true or false. Other left operands continue through expression emission, which retains the distinction between false and an exception. At function entry, the existing direct branch form now accepts any pair of supported type tests on argument registers.

The OTP oracle checks a nested conjunction inside an orelse, entry tests on different arguments, and a badarg in the left operand while the right operand would succeed. Both code paths use test and jump for supported cases. In the nested fixture the C# general guard still uses a stack frame, while OTP keeps the inputs in X registers.

The implementation reuses the C# guard emitter, auto-import resolution, and BEAM test encodings. The pinned OTP Core compiler in v3_core:gexpr uses a boolean switch to lower orelse. The comparison uses the release compiler output and existing test runner.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The full Windows OTP 29.1.1 run generated all 1,366 release images. The strict comparison reports 382 Equal, including ELDAPv3 generated from ASN.1 in C#, and 984 Different. Both runtimes accepted all 984 differing images. The emitter changed 40 images and 90 functions. The absolute Code size gap fell by 5,122 bytes in aggregate. Whole-image equality and normalized function equality did not change. In logger_formatter:do_format/4 the branch with hd(Key) increased the frame from 10 to 11 slots, with five slots in OTP.

The short-circuit scan dropped from 93 functions in 38 images to 20 functions in eight images. In httpc:cancel_request/2 the release guard now uses the same X-register tests and branch order. In wxFont:new/1 and wxLocale:new/1 the C# guard uses tests, with Y-register inputs and additional frame instructions still present.

A separate corpus scan found 7,856 functions in 848 images with a split tuple test and a deficit of matching fused tests in C#. The set includes 23 modules in the near-image ranking and 22 with the wx_env tuple shape.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release guard comparison](release-guards.txt), and [remaining guard differences](short-circuit-guards-after.txt)
- [Tuple test ranking](tagged-tuple-tests-after.txt) and [near-image overlap](near-tagged-overlap.txt)
