# ERTS documentation inputs

Extend the source overlay from [Part 941](../part-941/inputs.md), keeping its original directories. Set $sourceInputs to that overlay, $otp to the pinned Windows OTP 29.1.1 installation and $publicKeyHeaders to the previously generated public_key ASN.1 headers. Run from the repository root.

```powershell
$overlay = (New-Item -ItemType Directory 'artifacts/beam-backend/part972-source-inputs').FullName
Copy-Item (Join-Path $sourceInputs 'lib'),(Join-Path $sourceInputs 'erts') $overlay -Recurse
$archive = Join-Path $overlay 'erts-docs.tar'
git -C ../otp archive --format=tar "--output=$archive" OTP-29.1.1 -- erts/doc/src/erlang_system_info.md erts/doc/src/erlang_port_info.md
if ($LASTEXITCODE -ne 0) { throw 'Could not read the pinned ERTS documentation' }
tar -xf $archive -C $overlay
if ($LASTEXITCODE -ne 0) { throw 'Could not extract the pinned ERTS documentation' }
dotnet run --project tools/Validation/Erlang.BeamCorpus -- $otp artifacts/beam-backend/otp-corpus-part972.json $publicKeyHeaders $overlay
```
