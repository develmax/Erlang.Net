# HaltProbe: external owned-parent Windows minidump

Source-only tools correction. The helper did not execute builds, native calls, CLR processes, tests, original OTP, git mutations or CI. Root owns application, formatting, builds and acceptance. Runtime production source and all existing oracle files remain untouched.

The previous endpoint calls MiniDumpWriteDump on its own process from a new managed thread and joins it. Microsoft explicitly recommends using a separate process because an in-target call can encounter loader deadlock. A dedicated thread addresses stack exhaustion but does not remove all self-dump hazards. This is a source-supported cause candidate for the retained CI timeout; it does not prove which lock blocked that specific failed process. See [MiniDumpWriteDump remarks](https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/nf-minidumpapiset-minidumpwritedump).

The replacement launches a new process of this exact HaltProbe assembly using the parent's actual framework installation and explicit current CLR version. Its early internal command executes before Erlang setup. ProcessStartInfo uses argument lists, no shell and CreateNoWindow. Both redirected streams drain while it writes. Failure/timeout kills and physically joins the owned child, retaining stderr and partial dump rather than accepting incomplete output. The ordinary outer30-second process watchdog,300-second group budget and330-second test budget are unchanged; no timeout was increased or test skipped.

The child accepts only its actual creating parent from documented Toolhelp32 PROCESSENTRY32.th32ParentProcessID, then verifies that live parent's exact creation UTC ticks. Only then does it open QueryInformation|VMRead rights and call real MiniDumpWriteDump against that parent handle. The parent remains waiting until physical child exit. No arbitrary target PID or process discovery is accepted by the internal command. Source: [PROCESSENTRY32](https://learn.microsoft.com/en-us/windows/win32/api/tlhelp32/ns-tlhelp32-processentry32).

Both child and parent independently inspect the actual dump file, not a generated substitute: MDMP signature, bounded real stream directory/location extents, exactly one MiscInfoStream with the PROCESS_ID validity flag and exact live parent PID. Native process identity is retained in the sidecar requested-clr.dmp.identity.json. This strengthens the previous header-only test without pretending a CLR dump is an original BEAM crash dump. The [MINIDUMP_MISC_INFO layout and ProcessId validity flag](https://learn.microsoft.com/en-us/windows/win32/api/minidumpapiset/ns-minidumpapiset-minidump_misc_info) define the check. All raw minidump bytes come from DbgHelp; no binary header, stream or PID is patched.

Root integration:

```powershell
python -B artifacts/library437-halt-external-dump-proposal1/integrate.py --repository $PWD.Path
# Review the PASS7 exact targets, then Root may use --apply.
```

Two replacements: Program's early internal command and the Windows-only HaltPhysicalEndpoint writer. Five new single-type files: names, parent launcher, Toolhelp struct, child command, raw identity reader. No project/dependency changes are required (SDK includes these C# files automatically). Linux dump, abort and numeric exits remain unchanged.

Root acceptance must run the existing `runtime/halt-dedicated-raw-process-numeric-errors-abort-and-real-dump` group through both Interpreted and Compiled routes, preserve the original failed CI receipt, confirm no timeout/rescue kill and read each actual dump/sidecar parent PID against process-raw-evidence.json. The full existing contract count remains unchanged. A bounded negative helper control may invoke the early command with a PID different from the launching process and a fresh dump path: it must exit125 without creating any dump. Root alone executes that control; no helper execution has occurred.

Explicit limits: external dumping removes the documented self-target lock hazard but DbgHelp/OS failure still fails visibly. No guarantee is made that every machine completes within the fixed30seconds; the fixed watchdog remains the acceptance boundary. The internal parent check is an ownership invariant for this test tool, not an OS security sandbox for hostile same-user processes capable of spoofing native parent attributes.
