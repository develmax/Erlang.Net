# Part 1192 — evaluate shared tuple payload extraction

The candidate extracted the second tuple field once before select_val, then copied it into the selected branch's Y slot. The OTP and C# probes passed and the candidate matched OTP's extraction order in the probe.

The full candidate run generated 1,363 of 1,366 Windows OTP 29.1.1 images. It recorded 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP prepared all differing images for loading and three focused tests passed.

The candidate increased the Code length gap in all three affected modules. Disassembly shows instruction growth in beam_call_types:types/3, systools_make:mk_tar/5 and two snmpa_agent callbacks. Reverted the candidate. Part 1191 remains the compiler baseline.

- [Candidate corpus](candidate-corpus.json)
- [Candidate delta](delta.json)
- [Candidate focused tests](focused.json)
- [Candidate loader](loader.txt)
- [Changed functions](changed-functions.txt)
- [Probe disassembly](disassembly.txt)
