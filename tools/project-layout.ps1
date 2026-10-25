function Resolve-OriginalLibraryDirectory {
    param([Parameter(Mandatory)][string]$Library)

    $projects = @(Get-ChildItem -LiteralPath "$PSScriptRoot/../src/Otp/Original" -Filter "Erlang.Otp.$Library.Beam.csproj" -File -Recurse)
    if ($projects.Count -ne 1) { throw "Expected exactly one original OTP library project: $Library" }

    return $projects[0].Directory.FullName
}
