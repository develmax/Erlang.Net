
# Bootstrap library include provenance across five OTP applications

The exact Part1875 full corpus contains 101 raw Dbgi mismatches. Of these, 72 are entirely attributable to aligned file-path strings. After the separate Edoc package, the largest proven shared path rule is bootstrap application lookup after ordinary EPP search.

| Application | Pinned Makefile include order | Scoped sources | New exact raw Dbgi in replay | Retained existing Dbgi |
| --- | --- | ---: | ---: | ---: |
| Mnesia | Current and working directories, then bootstrap | 31 | 4 | 26 |
| SASL | Current and working directories, then -I../../stdlib/include, then bootstrap | 17 | 4 | 13 |
| Tools | Current and working directories, then bootstrap | 16 | 6 | 9 |
| Xmerl | Current and working directories, then -I../include, then bootstrap | 35 | 6 | 29 |
| Dialyzer | Current and working directories, then bootstrap | 29 | 5 | 24 |

OTP's EPP first tries the ordinary path list for both include and include_lib. Only the latter falls back to the code path application after unsuccessful ordinary lookup. OTP Makefile.in supplies the bootstrap tool and its primary kernel and stdlib application roots. The same Makefile's tertiary bootstrap list includes public_key and its staging rule copies the in-tree public_key include files to bootstrap. The app-specific src makefiles supply the ordinary include flags above. Xmerl adds an in-tree transform path but its application-qualified include in the measured sources addresses kernel. The explicit Dialyzer ../../compiler/src/core_parse.hrl comes from the logical working-directory mirror before any application fallback.

The isolated replay uses the exact generated option sequence, installed .erl files and the pinned header layout. Pinned ERTS EPP chooses the physical candidate independently. Logical root projection preserves the raw lexical path. It predicts all file transitions on all 128 scoped inputs. The raw section proof re-encodes only these predicted file-path replacements in the sealed C# abstract forms. It recovers 25 full raw Dbgi payloads, retains all 101 existing matching payloads and introduces no losses. Fifteen whole images are already Equal in the scoped baseline. mnesia_bup and xref_scanner retain independent annotation-column mismatches in create_dat_files/3 and lex/1.

The staged adapter reuses BeamIncludePathMap and the normal EPP search ordering. It enables the named profiles only for installed src modules with the exact makefile directory options. It places roots under bootstrap from actual build phase and declares no module or header filename substitutions. The referenced OTP makefiles and preprocessor are under Apache-2.0. No third-party code, dependencies, or reference BEAM data enter the compiler. The tagged reference checkout at OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5 was read only. The permanent fixture covers a regular SASL -I winner, public_key bootstrap provenance, and the local search shadowing bootstrap kernel. The staged C# patch passes layout and apply checks. No managed build or C# emission run was started during the gate.

