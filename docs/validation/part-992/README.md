# Part 992 wx integer and text overloads

Emit the shared tagged-record check and extract the object handle before dispatching between integer and chardata arguments. Reuse the existing command emitter for both branches.

The deterministic overload fixture matched OTP byte for byte. [Three focused checks passed](focused.json).

The complete Windows OTP 29.1.1 corpus has 272 Equal, 1091 Different and 3 SourceRejected. WxListBox and wxSpinCtrl are new matches. ELDAPv3 and the other previous matches remain Equal. [Corpus report](otp-corpus.json).

The wx opcode survey covers 235 generated modules. The largest remaining groups start with allocation where OTP emits a tagged-tuple or list test. [Opcode survey](wx-opcodes.txt).

The two largest opcode groups cover 47 distinct modules and 128 functions. A module can have several unrelated differences. [Affected functions](wx-priority-functions.txt).
