# Part 986 wx colour tuple commands

Match the OTP 29.1.1 is_colordata guard and lower the final wxe_util:color/1 argument through the command emitter. Check tuple shape and the first integer field before allocating the frame. Convert the validated argument before fetching the wx environment and calling queue_cmd. The instruction layout follows wxAuiTabArt:setActiveColour/2. No dependency was added.

The deterministic colour command fixture matches OTP byte for byte. All three focused checks passed. [Focused checks](focused.json).

In 235 generated wx images, 62 additional first frame sizes match. The first is_tuple versus allocate difference fell from 31 to 21 modules. Remaining prefixes include overload dispatch and chardata conversions on arguments followed by other parameters. [Frame counts](frame-counts.txt), [opcode comparison](wx-opcodes.txt), [tagged tuple prefixes](is_tagged_tuple-prefix.txt) and [list prefixes](is_list-prefix.txt).

The full Windows OTP 29.1.1 corpus has 266 Equal, 1097 Different and 3 SourceRejected. WxAuiTabArt, wxFontData, wxGridCellAttr and wxListItem are new exact matches. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
