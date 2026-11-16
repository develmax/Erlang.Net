# External documentation inputs

Use the source header overlay from [Part 930](../part-930/headers.md) as the starting point. Keep its release headers and add the six Markdown files from the pinned source tag. Run from the repository root with the pinned installation in $otp, the source header directory in $sourceHeaders and the generated public_key header directory in $publicKeyHeaders.

```powershell
$overlay = (New-Item -ItemType Directory 'artifacts/part941-source-inputs').FullName
Copy-Item (Join-Path $sourceHeaders 'lib'),(Join-Path $sourceHeaders 'erts') $overlay -Recurse
$paths = 'ms_transform','peer','qlc','re','shell','sofs' | ForEach-Object { "lib/stdlib/doc/src/$_.md" }
$archive = Join-Path $overlay 'docs.tar'
git -C ../otp archive --format=tar "--output=$archive" OTP-29.1.1 -- @paths
if ($LASTEXITCODE -ne 0) { throw 'Could not read pinned documentation' }
tar -xf $archive -C $overlay
if ($LASTEXITCODE -ne 0) { throw 'Could not extract pinned documentation' }
dotnet run --project tools/Validation/Erlang.BeamCorpus -- $otp artifacts/otp-corpus-part941.json $publicKeyHeaders $overlay
```
