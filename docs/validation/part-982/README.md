# Part 982 mixed wx option transformations

Emit direct, nested tuple and Unicode conversion cases in the same option dispatcher. Hoist the inner field when several branches use it, and emit mixed conversion targets in reverse postorder of the tag switch. The wxDirDialog release shows why source order is insufficient. The rejected branch retains the original tuple for badoption. No dependency was added.

A deterministic fixture with two Unicode conversions, a nested tuple and a pass-through option matches OTP byte for byte. All three focused checks passed. [Focused checks](focused.json).

In 235 generated wx images, another 38 first frame sizes match the release. The first is_tuple versus allocate divergence fell from 73 to 59 modules. The initial mixed implementation had branch-order divergences that disappear after the final ordering change. [Frame counts](frame-counts.txt), [initial branch diagnostic](initial-branch-order.txt) and [final opcode comparison](wx-opcodes.txt).

The full Windows OTP 29.1.1 corpus has 232 Equal, 1131 Different and 3 SourceRejected. WxDirDialog, wxFileDialog, wxGenericDirCtrl and wxPreviewFrame are new exact matches. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
