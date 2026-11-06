# Logger review follow-up

With info logging enabled, the original OTP raises function_clause for an
invalid message in logger:do_log_allowed/5. The pre-fix Managed result instead
reported logger:info/1. The permanent witness compares the full stack and all
five original arguments. It validates the caller, group leader and Logger ETS
table against live runtime identities before replacing those identities in the
comparison. The fixture pins the timestamp at 42.

The rebuilt Managed and Beam backends each passed all 156 observations at the
default backtrace depth and depths 0 and 3. The prior 155-case publication
reports describe the initial revision. All three current CI source admissions
passed again, and the staged source tree and descriptor plan hashes matched.
