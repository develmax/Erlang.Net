# Part 1426 — Lower caught local throws with an empty stack

The compiler follows local callers and their exception handlers. It emits raise/3 with an empty stack when the reason is caught and the handlers discard the original stack. Observable handlers, bound mismatches, escaped functions, cleanup and literal reason dispatch retain throw/1 in the tested cases.

The deterministic caught-throw fixture matches OTP byte for byte. Both binaries pass the handler controls, including the unmatched reason, function reference and cleanup. The standalone .NET test initially lacked admission for the lists import. The final test declares that dependency and all three selected tests pass with the corpus compiler assembly.

All 1,366 Windows OTP 29.1.1 release images were generated with zero source rejections and generation failures. The corpus has 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading.

The throw and raise call counts change in 37 functions across 29 release images. The final audit finds zero changed functions with more raise calls than the release. collect_string/3 gains normalized instruction equality. The added raise arguments increase the aggregate Code size gap by 322 bytes.

The remaining opcode audit lists 555 functions across 156 modules. Of these, 513 functions in 153 modules use an ordinary external throw call. The next case uses a call to construct the throw reason in collect_quoted/3. Canonical readiness awaits a passing full report.

- [Corpus](otp-corpus.json), [normalized instructions](normalized-functions.txt), and [loader and Code sizes](loader-and-code.txt)
- [Throw delta](throw-delta.txt), [initial excess calls](initial-overlap.txt), [remaining candidates](remaining-throw-candidates.txt), and [call forms](throw-forms.txt)
- [Fixture oracle](check-caught-throw.ps1), [result](probe.txt), [handler oracle](check-throw-stack-guards.ps1), and [handler results](handler-probe.txt)
- [Collector instructions](collect-string.txt), [previous fixture compiler](previous-compiler.txt), [focused tests](focused.json), and [initial test](first-focused.json)
