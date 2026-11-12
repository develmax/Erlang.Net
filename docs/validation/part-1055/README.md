# Part 1055 preserve only live inputs across lookup cases

The register lookup case emitter preserves the bound key and inputs used by the empty-result tail call. It tests the singleton list, tuple arity, empty tail and exact key match in X registers. The original result remains available for case_end if matching fails.

The generated asn1_db:get_table/3 frame shrank from eight slots to two. Its decoded instructions and line locations match OTP after label normalization. The OTP execution probe verified a matching record, an empty result and the original unexpected result in a case_clause exception. The generated asn1_db image shrank by 40 bytes and still differs from the release.

The corpus admitted and generated 1363 of 1366 files. All per-file statuses remained unchanged at 344 Equal, 1019 Different and 3 SourceRejected. ELDAPv3 remains Equal. All 1019 differing images prepared successfully in OTP.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Get table instruction comparison](get-table.txt)
- [Executed probe](after.txt)
