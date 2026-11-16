# Part 1557 — Propagate lookup result types into tuple tests

Normal returns from lists:keyfind/3 and maps:find/2 or maps:take/2 are atoms or tuples. The C# compiler now propagates that union through local functions and imported lookup calls. Tuple tests on local calls use the union when inference proves it. The rules follow beam_call_types.erl from OTP 29.1.1 and reuse the existing BEAM type encoder. The change adds no dependency.

The instructions in ssl_trace:modules/1 and systools_make:var_tar_flag/1 match OTP after label normalization. The tagged tuple test in pubkey_cert:current_any_policy_qualifiers/1 still lacks a type operand. The execution fixture matched five outcomes on the OTP VM, including missing keys and a tuple of the wrong arity. Three selected compiler tests passed. [Modules](modules.txt) · [Flags](var_tar_flag.txt) · [Tagged projection](current_any_policy_qualifiers.txt) · [Fixture](beam_lookup_result_type.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. Fifteen images changed. Two functions gained a complete instruction match after label normalization, and no function lost one. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Instruction comparison](normalized-functions.txt) · [Loader and Code sizes](loader-and-code.txt).

The final run used the OTP source overlay root. An earlier run pointed the header argument at its nested erts directory and rejected 19 sources with missing includes.
