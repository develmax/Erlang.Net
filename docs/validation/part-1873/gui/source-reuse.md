
# In-tree OTP include directories

The sealed Part1870 corpus contains 182 raw Dbgi differences. Pinned ERTS re-encoding recovered 153 exact raw chunks when changing only the aligned file-path strings in the generated abstract forms. That diagnostic identifies paths as the bottleneck independently of instructions or line annotations.

The pinned Observer, Reltool and Et makefiles supply -I $(ERL_TOP)/lib. Observer also supplies -I../include, -I../../et/include and the adjacent external libraries/et directory in that order. The installed validation input supplies the in-tree Et source and no second Et source tree. The observer etop.hrl header includes runtime_tools/include/observer_backend.hrl through normal EPP lookup. Et's in-tree headers and the shared wx headers are also reached through the ordinary include path. EPP only uses code:lib_dir application fallback after all ordinary candidate lookups fail.

| Pinned source | Evidence |
| --- | --- |
| lib/observer/src/Makefile | The ordered include switches and in-tree wx code directory. |
| lib/reltool/src/Makefile | The in-tree library include directory. |
| lib/et/src/Makefile | The application include directory, in-tree library include directory and Et and wx code paths. |
| lib/observer/include/etop.hrl | A nested application-qualified runtime_tools include. |
| lib/stdlib/src/epp.erl at enter_file and scan_include_lib1 | Normal candidate ordering precedes library fallback for include_lib. |
| lib/kernel/src/file.erl at path_open_first | Preserves the winning candidate's lexical path. |

The generic C# candidate models a physical child directory alias as part of the existing search directory. Ordinary lookup first checks the supplied physical path and can then find the renamed child at that same search priority. The logical recorded path still uses the build application's unversioned source directory. The profile adapter applies only to the three audited release applications. It builds physical aliases from the corpus application directories and rejects ambiguous installed versions. No reference BEAM metadata is a compiler input.

The pinned source is OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. Its audited Erlang sources and makefiles carry Apache-2.0 notices. The local changes reuse the existing C# include resolver and .NET path APIs. They add no dependencies and copy no OTP implementation code or release bytes.

The complete 59-module cohort includes all Observer, Reltool and Et modules. All 34 changed files are byte-identical to their baseline output outside Dbgi. All 34 changed Dbgi chunks now match the corresponding Windows OTP raw chunks. There are 19 full-file Equal, 40 Different, zero Unsupported and zero SourceRejected in both lanes. ERTS 17.1 prepared all 40 generated files for loading, and ran the extended fixture to verify ordinary and library-qualified aliases and precedence.

