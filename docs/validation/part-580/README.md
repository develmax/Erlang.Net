# PR109 follow-up: original common_test/dialyzer boot

The Windows job [126570358234](https://github.com/develmax/Erlang.Net/actions/runs/41828811531/job/126570358234) on `0d492c8e0394b39a0784bb962fb3c7d231d18ed7` passed the repaired source admissions but failed `validate.ps1` in `Erlang.Otp.TestAnalysis.Library.Tests`, at the whole-outcome comparison.

The failure reproduces locally. Original `ct_groups:make_conf/4` calls the selected original `code:is_loaded/1`, which reaches `code_server:is_loaded/1`. The test's partial boot started only `file_server_2`; its missing `code_server` ETS table produced `badarg` instead of the original returned configurations. The former unconditional import shortcut had concealed this missing dependency.

The fix starts the unchanged original `code_server` through a boot `kernelProcess` instruction. The unchanged observer image is written into the boot directory and loaded by original `init` through `primLoad`, so `init:fetch_loaded/0` reports its filename to the server. It is no longer prepublished outside boot. Assertions check its absence before boot, actual BEAM ownership afterward and the live server. The original 2463-case ETF and observer sources/BEAM remain unchanged.

Validation: ordinary build of the affected test project, no source exclusions; the entire common_test/dialyzer group matches all 2463 original outcomes/stacks and identical raw ETF bytes. `testanalysis.json` records source/result hashes and the local failing/successful comparison. Full local regression was not run. Hosted follow-up CI remains a separate gate.

PR109 was already merged externally as `91c05943d4dba4c912a76f80ac5e6d778bffe4b3` while its checks were still running. This follow-up targets current `main`; it does not imply approval, merge, review resolution or broader compatibility promotion.
