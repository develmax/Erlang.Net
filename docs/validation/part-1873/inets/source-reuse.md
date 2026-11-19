
# Windows inets include provenance

The four source directories in the OTP 29.1.1 inets application have distinct include options. The pinned http_client, http_lib and http_server makefiles establish their ordered directories. Their common inets_app/inets.mk adds the in-tree inets ebin to the bootstrap compiler's code path. Relative include names resolve from the current source or header. Selected -I paths retain their lexical parent components. An unresolved library-qualified inets name uses the in-tree application root. A kernel name falls back to the bootstrap application.

The model reuses the existing C# BeamIncludePathMap. It pairs each ordered, already-recorded include option with its corresponding installed directory. It distinguishes the pinned inets in-tree fallback from the bootstrap kernel fallback. The tagged bootstrap and source versions of file.hrl, inet.hrl and logger.hrl have equal Git blob identities. The optional source-header tree mirrors the same logical current directory. There are no per-module or per-header exceptions.

The audited Makefiles and the pinned compiler, EPP and file resolver carry Apache-2.0 notices. This change adds no dependency and copies no implementation or oracle data. The reference checkout was read through git show OTP-29.1.1 and unchanged. Release artifacts are used only in validation.

All 63 inets release modules were compared with the same sealed Part1870 C# compiler DLL. All 24 modified files changed only their Dbgi payload. Every replacement was byte-identical to Windows OTP. Both lanes have 12 Equal, 51 Different, zero Unsupported and zero SourceRejected. All 51 generated modules passed ERTS 17.1 prepare-loading. An isolated executable fixture verified explicit relative include, ordered -I, source-local shadowing and the two distinct application identities. The focused comparison took 12.377 seconds at one worker.

The patch applies to the sealed Part1870 tree and after the Observer, Reltool and Et in-tree include patch. The only shared file is the corpus tool Program.cs. Its adjacent dispatch insertions use distinct application scopes. The 63 inets modules and the 59 GUI cohort modules are disjoint.

