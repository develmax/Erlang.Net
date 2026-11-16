# Part 1041 reuse of temporary slots between clauses

The general emitter restarts temporary slot assignment at the end of the shared argument and closure slots for each clause. The frame still uses the maximum number of slots reached on any clause. The same slot can hold independent values on mutually exclusive paths.

The OTP execution probe checks guard success, guard fallthrough, pattern fallthrough and process dictionary effects. It passed before and after the change. Its frame shrank from six slots to four.

| Previously rejected function | Generated slots before | Generated slots after |
| --- | ---: | ---: |
| unicode_util:gc_h_lv_lvt/3 | 2404 | 6 |
| megaco_erl_dist_encoder_mc:e/2 | 1930 | 22 |
| erl_erts_errors:format_erlang_error/3 | 1067 | 24 |
| beam_validator:vi/2 | 1030 | 26 |
| beam_disasm:resolve_inst/4 | 1679 | 38 |

OTP 29.1.1 now prepares all 1021 differing generated images. This checks loader preparation without installing the modules or executing on_load. The full corpus admitted and generated 1363 of 1366 images. All byte comparison statuses remain unchanged at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Selected frame sizes against OTP](compare-frames.txt)
- [Corpus frame counts before](frames-before.txt)
- [Corpus frame counts after](frames.txt)
- [Execution probe](clause_frame_probe.erl)
- [OTP probe assertions](run-probe.eval)
