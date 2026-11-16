# Part 1109 — forward several tagged tuple fields in X registers

A terminal case can unpack the tagged result of an ordinary call and tail-call with the projected fields and constants. The input tuple remains in X0 until the last field extraction. Tail argument permutation uses the existing ordered moves. A wildcard branch can still return a constant. The register path rejects patterns that would compare a field against an existing binding or a repeated variable.

The OTP 29.1.1 probe matched the instruction lists for both local tail calls, including their differing argument order. The remote probe returned the same map value, fallback and badkey error. OTP uses the map_get BIF while the current emitter uses a tail call to maps:get/2.

- [Probe source](beam_entry_fields_probe.erl)
- [Execution and disassembly assertions](run.eval)
- [Instruction comparison](disassembly.txt)

The next dispatch probe shows two OTP layouts. Equal-arity branches share field extraction and dispatch by tag with select_val. Mixed arities dispatch first with select_tuple_arity.

- [Next dispatch probe](beam_entry_tag_dispatch_probe.erl)
- [OTP dispatch instructions](dispatch-otp.txt)

The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The Code length gaps narrowed in code_server, beam_lib and dets_server, the only images that changed. OTP accepted all differing images during loader preparation.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
