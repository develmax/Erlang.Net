# Math CI receipt verification

Run [41631118602](https://github.com/develmax/Erlang.Net/actions/runs/41631118602) passed for PR #102 head 20e02eb941ef3b99cb675827ef0f20663da7c04b. GitHub compare confirmed that the checked-out synthetic merge b1860e7a44158b07ecb0fd75737987568f4ac94e has the same tree.

Windows x64, Ubuntu x64 and macOS ARM64 each passed both explicit backends: 502,772 numeric/type/error outcomes, 248 complete value/error cases and five full-stack profiles per backend. All three downloaded ZIP SHA-256 values match GitHub artifact digests. All eight oracle receipt files in each archive passed independent byte-length and SHA-256 verification. Reports preserve actual host/runtime/oracle/image identities.

See verified-ci-summary.json. Production and validator sources were frozen for integration; the binding comparison is recorded. These receipts belong to the stated head. They are not a substitute for checks and review of a subsequent integrated head or proof for arbitrary custom CRT/OTP builds. Canonical ledger and shared checkpoint updates remain with the integration owner.
