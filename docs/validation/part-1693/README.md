# Part 1693

The structural list emitter groups an initial prefix of tagged tuples by arity when the clauses end in an unconditional cons fallback and each arity uses a single tag. The table is ascending by arity and the blocks are descending. Clause and guard priority within each arity is preserved. OTP 29.1.1 beam_core_to_ssa group_value and the release disassembly were used as references. The emitter reuses the existing typed arity selector and branch lowering. No dependency was added.

All 1,366 sources were accepted and generated. 398 images were byte-identical and 968 differed. The C# ASN.1-generated ELDAPv3 remained byte-identical. All differing images passed the OTP loader. Nine changed functions now have the same count of select_tuple_arity instructions as OTP. Their Code gaps grew by 27 to 34 bytes and no full normalized function match changed. [Corpus](otp-corpus.json) · [Dispatch audit](dispatch.txt) · [Loader and Code](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

The C# compiler and test harness built with zero warnings and errors. The OTP VM checked both arities and failure paths. Three focused tests passed, including permanent coverage of reordered arity blocks and guard, tag and selection fallbacks. [Fixture](beam_shared_cons_payload.erl) · [Disassembly](disassembly.txt) · [VM checks](outcomes.txt) · [Tests](focused.txt).

The Part 1692 baseline was also disassembled by function and opcode. Extra moves occur in 967 differing images and missing trim instructions in 867. These are frequency counts and do not establish that one optimization would make those modules identical. [Opcode counts](opcode-baseline.txt).

Full runtime tests and readiness regeneration were outside this run.
