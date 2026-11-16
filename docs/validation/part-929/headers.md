# Restoring public_key ASN.1 headers

The Windows OTP 29.1.1 installation ships the generated public_key Erlang modules and their ASN.1 inputs. Most generated private headers are absent. Restore these compiler inputs in a fresh workspace directory using the pinned release ASN.1 compiler. The source build flags are in lib/public_key/asn1/Makefile at tag OTP-29.1.1.

Run from the repository root with the pinned installation path in the PowerShell variable $otp.

```powershell
$schema = Join-Path $otp 'lib/public_key-1.21.7/asn1'
$headers = (New-Item -ItemType Directory 'artifacts/part929-public-key-headers').FullName
$schemas = Get-ChildItem $schema -Filter '*.asn1' | Sort-Object Name
& (Join-Path $otp 'bin/erlc.exe') -bber +der +noobj +asn1config -I $schema -o $headers $schemas.FullName
if ($LASTEXITCODE -ne 0) { throw 'OTP ASN.1 header generation failed' }
dotnet run --project tools/Validation/Erlang.BeamCorpus -- $otp artifacts/otp-corpus-part929.json $headers
```

Both alphabetic order and ASN_MODULES order from the pinned Makefile produced the same 29 headers byte for byte in fresh output directories. No BEAM files were produced by that preparation step. The corpus tool supplies the header directory only to public_key modules and compiles their installed Erlang sources using the C# compiler. ELDAPv3 continues to use the C# ASN.1 generator.
