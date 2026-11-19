# Adjacent GC BIF results in X0

The candidate applies the existing single-use stack-copy rewrite to an ordinary GC BIF that produces X0 on fallthrough. It preserves the original register type, increments the consumer's X root prefix where necessary and initializes the old Y slot until frame cleanup can prove it redundant. The dataflow proof rejects subsequent reads, handler regions and frame trims. The two-case test compiles one fixture and checks both outcomes with raw Code bytes and managed preparation.

The oracle is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The relevant original stages are Y interval reservation in beam_ssa_pre_codegen, X clobber classification in beam_ssa and GC live-prefix handling in beam_ssa_codegen. Git excerpts are saved in this directory. This is a C# emitter fallback for the existing pipeline, with no dependency added or upstream source imported. The sibling OTP checkout was read through git show.

The base is the Part1848 frozen source archive and compiler. Hashes for the source archive, compiler binaries, release module source, release and generated BEAMs, cohort reports, module list and patch are in identity.json. SDK 10.0.400 and .NET 10.0.11 ran on Windows x64. The frozen corpus host reused the Part1845 BeamCorpus executable with only its compiler DLL replaced in each separate scoped lane.

| Verification | Before | Candidate |
|---|---|---|
| Five-module scoped byte comparison, jobs 1 | 4 Equal, 1 Different, 0 Unsupported, 0 SourceRejected | 4 Equal, 1 Different, 0 Unsupported, 0 SourceRejected |
| Four-module follow-up from saved-image scan, jobs 1 | 0 Equal, 4 Different, 0 Unsupported, 0 SourceRejected | 0 Equal, 4 Different, 0 Unsupported, 0 SourceRejected |
| Five-module comparison time | 8.49 s | 10.34 s |
| Four-module comparison time | 17.08 s | 17.75 s |
| diameter_session raw Code bytes | 660 | 649 (OTP 549) |
| diameter_session:time32/1 disassembly with register types omitted | Different | Equal |
| diameter_session native preparation and fixture calls | Pass | Pass |

No full matches were gained or lost. diameter_session:time32/1 uses one fewer stack slot and has one additional X root at its final allocating BIF. The candidate's final band operand lacks OTP's t_number annotation. The module has further differences in init/0, sequence/1, session_id/1 and metadata. Two other functions in the release module changed in the candidate. Seeded Erlang execution checks both init/0 and sequence/1 against the release image, including ETS values computed from the actual timestamp and known random bits. The native fixture checks timestamp and later-read variants, arbitrary precision and badarith. All three versions agreed.

The saved Part1845 data had 420 lexical shapes across 142 of 960 different modules. This is a bound on a GC BIF consumer shape in old generated images. It does not measure successful rewrites, prove later-read safety for those matches or include the map consumer variant. Fresh native checks pass for array, base64, rand and beam_bounds in the four-module follow-up. None of the changed bodies in these four modules reached equality with OTP even after removing type suffixes. Some Code chunks grew with conservative slot initialization. All results, native invocations and stderr are saved separately from inputs.

The focused test passes with the candidate and fails at its assertion when loaded with the pinned Part1848 compiler. The scoped SDK-backed Erlang.Style check passes. The full regression and corpus remained with root integration. Suggested next diameter dependency is preserving parallel X values when an arithmetic result and its prior argument both survive until the next BIF, followed by typed numeric propagation and attribute/literal evidence.
