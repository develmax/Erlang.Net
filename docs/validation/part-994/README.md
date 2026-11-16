# Part 994 wx text conversion and tuple guards

Reuse the existing forward-guard emitter for tuple field tests alongside chardata tests. Keep the converted text in its original stack slot when a tuple follows the text and the command maps its options.

The deterministic middle-text fixture now covers tuple field guards both with and without mapped options. The instructions of drawText/3, drawRotatedText/4 and drawLabel/4 match OTP after normalizing local labels and closure identifiers. [Function comparison](wxdc-functions.txt). [Three focused checks passed](focused.json).

The complete Windows OTP 29.1.1 corpus has 273 Equal, 1090 Different and 3 SourceRejected. ELDAPv3 remains Equal. WxDC still differs in its complete image. [Corpus report](otp-corpus.json).

The wx survey covers 235 generated images. The list-test-before-allocation difference dropped from 30 to 29 modules. Six of the eight remaining shared-function differences in wxDC use colour guards. [Opcode survey](wx-opcodes.txt). [Remaining wxDC differences](wxdc-residual.txt).
