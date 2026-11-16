# Restoring source-tree include inputs

Use the same public_key header directory as in [Part 929](../part-929/headers.md). The additional directory holds selected tracked headers from the pinned OTP checkout with their original relative paths. This preserves relative sibling lookups for common_test and dialyzer even though the installed application directories carry version suffixes.

Run from the repository root. Set $otp to the pinned Windows installation and $publicKeyHeaders to the generated public_key header directory.

```powershell
$paths = @(
    'lib/common_test/src/test_server_internal.hrl'
    'lib/ftp/src/ftp_internal.hrl'
    'lib/kernel/src/socket_int.hrl'
    'lib/kernel/src/logger_internal.hrl'
    'lib/compiler/src/core_parse.hrl'
    'erts/preloaded/src/zip_internal.hrl'
)
$overlay = (New-Item -ItemType Directory 'artifacts/part930-source-headers').FullName
$archive = Join-Path $overlay 'headers.tar'
git -C ../otp archive --format=tar "--output=$archive" OTP-29.1.1 -- @paths
if ($LASTEXITCODE -ne 0) { throw 'Could not read pinned headers' }
tar -xf $archive -C $overlay
if ($LASTEXITCODE -ne 0) { throw 'Could not extract pinned headers' }
dotnet run --project tools/Validation/Erlang.BeamCorpus -- $otp artifacts/otp-corpus-part930.json $publicKeyHeaders $overlay
```

Pinned source commit is ad05823719d77c8faee87348ea39513d4e2f99c5. The corpus runner reads the external directories only as include roots and takes source modules and reference BEAM files from the release installation. The C# ASN.1 generator still produces the ELDAPv3 Erlang source.
