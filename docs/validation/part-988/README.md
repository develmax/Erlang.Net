# Part 988 wx Unicode list helpers

Extract the closed Unicode comprehension from wx command sequences and reuse the existing recursive mapping helper. Match the generator line, recursive call, proper list cases and bad_generator path in the OTP 29.1.1 instructions. The command lowering uses the same Y-slot handling as text conversion, with a local call to the extracted helper. No dependency was added.

The deterministic wx Unicode list fixture matches OTP byte for byte. All three focused checks passed. [Focused checks](focused.json).

All four appendStrings and insertStrings functions and their four helpers in wxControlWithItems match the reference instruction lists after normalizing local labels. The helper order still differs. OTP places the findString closure between the two pairs of comprehension functions. [Instruction comparison](helper-instructions.txt), [helper order](helper-order.txt) and [wx opcodes](wx-opcodes.txt).

The full Windows OTP 29.1.1 corpus has 266 Equal, 1097 Different and 3 SourceRejected. The exact set is unchanged and includes ELDAPv3. [Corpus](otp-corpus.json).
