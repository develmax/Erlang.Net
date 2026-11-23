# Remove a unique unused literal binding in a tuple

The source is the Part 1845 freeze on Windows OTP 29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler prototype is separate on D: and uses the frozen runtime dependencies and pinned SDK 10.0.400.

The original C# output materializes each field in an entirely literal ETS instruction tuple when the source uses nested aliases such as _Pos = 2. OTP Core substitution eliminates the unobserved names. The pinned sys_core_fold:let_subst_list and pattern scope rules are the algorithm references. The C# compiler applies an equivalent conservative source simplification before BEAM lowering. It only replaces a direct tuple-field match when the variable name occurs exactly once in the clause, the value normalizes as a constant, and the clause has no nested binding scopes. It counts parameter patterns, match patterns and all variable reads. Existing bindings, subsequent reads, effects and compile optimization controls are preserved.

In the seven-module cohort, 4 images remain Equal and 3 remain Different, with no Unsupported or SourceRejected. diameter_session and xmerl_scan change. diameter_traffic and the four exact controls stay byte-identical to their previous outputs. The baseline comparison took 36.125 seconds and the candidate took 41.046 seconds at jobs=1 on the shared host.

- The complete diameter_session:sequence/0 function disassembly now equals OTP. The diameter_session literal table becomes exact. In session_id/1 the instruction tuple is literal and the initial frame shrinks from 4 to the pinned 3 Y slots, while later arithmetic lowering still differs. Module Code shrinks from 662 to 583 bytes at an OTP target of 549.
- The xmerl_scan:scan_xml_decl3/3 body changes independently of the closure checksum update. Its module Code shrinks by 15 bytes.
- All six baseline, candidate and OTP images for the two changed modules prepare successfully in OTP. Native diameter ETS rollover, sequence dispatch and session ID results match on the three images. Three xmerl XML parses have the same deterministic decoded-term digest.
- The exact one-function fixture is byte-identical to OTP after the change (SHA256 609F789DB62BC171C6837224F1DFCEEA2B4F7A84BEC6BABDA7AC772C2637A25D). The permanent mixed fixture fails on the frozen compiler and passes on the candidate. Its six native results also match, including the existing binding's badmatch, the later read and a process-dictionary effect.
- The isolated compiler build has zero warnings and errors. Scoped source layout passes. Standard full regression and a new full corpus were outside this focused checkpoint.

Patch SHA256 is 69E19A4525477FAA7E47163164BECCEF87CB1D439EB8D07B265E63A0BFF3DC9D. Forward and reverse checks validate it at the frozen base. The linked integration worktrees and the reference checkout were read-only.
