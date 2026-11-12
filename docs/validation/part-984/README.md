# Part 984 wx commands with chardata and mapped options

Lower object commands that convert the penultimate chardata argument and map the final options list. Preserve input guard order and reuse the existing reverse-order frame. Initialize the consumed text slot before the conversion call and reuse it for the converted result. The release loadPerspective/3 allocation now matches. No dependency was added.

The deterministic fixture with two object arguments, chardata and mapped options matches OTP byte for byte. All three focused checks passed. [Focused checks](focused.json) and [allocation sample](load-perspective.txt).

In 235 generated wx images, 70 additional first frame sizes match. The first is_list versus allocate divergence fell from 57 to 35 modules. The largest remaining prefix group includes static commands such as wxArtProvider:getBitmap/2. [Frame counts](frame-counts.txt), [opcode comparison](wx-opcodes.txt) and [remaining list prefixes](list-prefix.txt).

The full Windows OTP 29.1.1 corpus has 260 Equal, 1103 Different and 3 SourceRejected. Sixteen more wx modules match byte for byte. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
