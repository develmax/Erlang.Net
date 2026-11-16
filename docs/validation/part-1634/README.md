# Part 1634

Short list append can construct its result directly in the X register required by the following call. The unframed tail-call planner recognizes append arguments and reuses the existing cons construction planner. The general emitter passes the desired register through append lowering. Pinned OTP release disassembly and the existing list emitters provide the reference. No dependency was added.

The regression fixture and OTP BEAM match byte for byte. The OTP VM verified proper and improper tails. All three focused tests passed. [Disassembly](disassembly.txt) · [Tests](focused.txt) · [VM outcome](outcomes.txt).

The compiler accepted and generated all 1,366 Windows OTP 29.1.1 images. 394 matched, including ELDAPv3 and Observer. The other 972 differed and passed OTP loading. All 178 changed images had a smaller Code size gap, with an aggregate reduction of 2,260 bytes. Three xmerl_scan helper functions matched after label normalization with no losses. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

In httpd_file:handle_error/5 the cons destination registers now match, and the module Code size gap fell from 42 to 30 bytes. Frame allocation, line instructions and other sections still differ. The full test suite and readiness regeneration were outside this run. [httpd_file disassembly](httpd-file-disassembly.txt).
