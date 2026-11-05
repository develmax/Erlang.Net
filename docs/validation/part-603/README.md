# Part603 DETS continuation validation

Validated on Windows with .NET SDK 10.0.400. The tested production tree is
`781708289a25dfed44a358d691b765bde02a8293`. The platform test runner blob is
`b77a18f09ea304bf47f50856f5654e2941fe5c92`. The reference is OTP 29.1.1,
commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

| Check | Command or report | Result |
| --- | --- | --- |
| Solution build | `dotnet build -m:1 --no-restore` | Passed with zero warnings and errors |
| Source layout | `Erlang.Style --check .` | Zero files need formatting |
| Focused runtime and compiler regressions | `Erlang.Tests --case <each ExactCases entry> --report <path>`, [report](focused-tests.json) | 45/45 passed |
| Platform runner build | `dotnet build tools/Validation/Erlang.DetsPlatform -m:1` | Passed with zero warnings and errors |
| Managed platform checks | `dotnet run --no-build --project tools/Validation/Erlang.DetsPlatform -- . artifacts/dets-select-review-final`, [report](managed.json) | 85 checks passed |
| Original DETS platform checks | Same platform runner, [report](original.json) | 86 checks passed and file interchange passed for set, bag and duplicate_bag |
| DETS storage regressions | [Report](review-regressions.json) | Fixed growth, FREE collection decoding and bad_object for an unknown status passed |

The managed platform scenario fixes a table, reads one row with select/3,
deletes the unread row and resumes with select/1. The continuation yields the
retained row and then end of table. Direct lookup confirms that the row remains
deleted. The separate header regression writes an invalid status to the known
collection address and requires bad_object.

The full regression suite and global readiness regeneration were outside this
focused validation. Hosted workflows are tracked in the pull request.
