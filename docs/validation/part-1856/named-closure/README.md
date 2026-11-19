# Managed verification of direct-only named self

SDK 10.0.400 built the staged candidate and standalone managed harness with zero warnings and errors. One BelowNormal worker used `DOTNET_PROCESSOR_COUNT=1`; each release run used `--jobs 1` and fresh output paths. `receipt.json` identifies the selected modules, compiler/tool DLLs, fixture images, and patch. No compiler result cache was used.

The candidate passed the named-self regression. The baseline reached the intended assertion and failed because `direct` allocates its inner self value. The managed fixture checks five direct-only cases, four value-use controls, and ten runtime results. The baseline and candidate fixture images are different from the pinned oracle image, whose behavior and shape are independently listed. Each generated fixture prepared, loaded, and matched all ten pinned native outcomes.

| 9 Windows OTP modules | Equal | Different | Unsupported | SourceRejected | Comparison |
| --- | ---: | ---: | ---: | ---: | ---: |
| Frozen Part 1855 retake | 1 | 8 | 0 | 0 | 60.17 s |
| Candidate | 1 | 8 | 0 | 0 | 58.23 s |

There are no newly equal or lost release images and no normalized-function gains or losses. Five images changed only `Code`, `Attr`, and `FunT`. The raw `FunT` comparison preserves atom index, total arity, lambda index and free count, with the module atom table identical. The manifest records both images for each module and leaves entry offsets and uniqueness outside that comparison. The other four module images, `beam_ssa`, `dbg_ieval`, `erl_eval`, and `logger_handler`, are unchanged byte-for-byte.

| Changed module | Named bodies with a removed self allocation |
| --- | ---: |
| `dialyzer_typesig` | 1 |
| `application_controller` | 3 |
| `code` | 1 |
| `dbg` | 1 |
| `io_ansi` | 5 |

No generated function keys changed. All 11 bodies retain the same direct self-call count. Two changed bodies have the same name and internal arity as their OTP counterparts, which have no body self allocation. The remaining nine still use distinct helper names relative to OTP. `self-bodies.terms` records each name, lifted arity and allocation/direct-call count. Pinned ERTS prepared all five release images. Release application entrypoints were not executed in this scoped lane. The native behavior fixture separately exercises capture, dynamic application, arity errors and catch boundaries.

This is one comparison per compiler and does not establish a performance change. Compiler naming, full-corpus acceptance and the package workflow remain separate. Earlier staged source-only evidence is preserved one directory above.
