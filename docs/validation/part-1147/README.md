# Part 1147 — build nested tuple branch results in registers

The terminal case planner builds nested tuples from the selector, saved arguments and literals. It reserves heap space before extraction or construction, and reuses X0 after the selector's final use.

The nested_result/2 probe matches OTP 29.1.1 after local label normalization. Both images returned the expected tagged result and nested fallback. In the asn1ct test_value closure, the frame shrank from six slots to four and the fallback tuple construction matches. The tagged branch still extracts into X0 and moves the field to X3 for the tail call.

- [Probe source](beam_general_catch_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Release comparison](asn1ct-test-value.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Six functions gained instruction matches and none lost one. All 23 changed images have smaller Code size gaps.

- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Function comparison](function-comparison.txt)
- [Image delta](delta.json)
- [Loader results](loader.txt)
