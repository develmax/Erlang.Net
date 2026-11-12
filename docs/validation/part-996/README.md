# Part 996 wx sequential Unicode argument conversions

Emit consecutive Unicode conversions in source order and preserve each converted value in its original argument slot before mapping options. The existing list conversion can follow multiple text conversions. The deterministic fixture now exercises two text arguments followed by a list.

The complete Windows OTP 29.1.1 corpus has 278 Equal, 1085 Different and 3 SourceRejected. WxHtmlEasyPrinting, wxMultiChoiceDialog and wxSingleChoiceDialog are new exact images. ELDAPv3 and all previous matches remain Equal. [Corpus report](otp-corpus.json). [Three focused checks passed](focused.json).

The list-test difference before allocation dropped from 27 to 23 modules in the survey of 235 generated wx images. WxHtmlWindow and wxImage also changed and still differ as complete images. The remaining shared-function instruction differences in wxHtmlWindow are in setRelatedStatusBar/2 and the new/2 option closure. [Opcode survey](wx-opcodes.txt). [Targeted results](wx-results.txt).
