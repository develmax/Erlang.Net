# Part 1168 — preserve selector types and match the SSL cache image

The closed tuple and atom case uses the existing inferred selector type. It omits the atom comparison only when the known call has that single atom result outside the tuple branch. This restores the two lists:keyfind instruction matches from before part 1167.

The full run generated 1,363 images and returned 351 Equal, 1,012 Different and three SourceRejected. ssl_server_session_cache_db.beam was the only status change and now matches Windows OTP 29.1.1 byte for byte. Fourteen still-different images changed. Five Code size gaps narrowed and none widened. Seven functions gained an instruction match and none lost one. ELDAPv3 remains Equal. OTP prepared all differing images for loading. All three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Instruction comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Tuple and atom oracle probe](../part-1167/disassembly.txt)
