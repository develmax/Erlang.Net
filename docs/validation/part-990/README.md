# Part 990 wx option Unicode list helpers

Extract Unicode list comprehensions from wx option closures and call the existing recursive mapping helper from the option branch. Keep the lexical owner so closure and comprehension ordering follow OTP 29.1.1. Verify a deterministic fixture with nested options, direct Unicode conversion and list conversion. No dependency was added.

All three focused checks passed. Four of the five missing comprehension helpers in the generated wx cohort now emit. The remaining helper belongs to wxAcceleratorTable and discards the result of a class-check traversal. The remaining opcode differences in wxChoice and wxComboBox are in create overloads. [Focused checks](focused.json), [opcode comparison](wx-opcodes.txt), [remaining helper](remaining-list-helpers.txt) and [remaining commands](remaining-option-commands.txt).

The full Windows OTP 29.1.1 corpus has 268 Equal, 1095 Different and 3 SourceRejected. WxCheckListBox is the new exact match and ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
