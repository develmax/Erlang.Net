function Resolve-OriginalLibraryDirectory {
    param([Parameter(Mandatory)][string]$Library)

    $projects = @(Get-ChildItem -LiteralPath "$PSScriptRoot/../src/Otp/Original" -Filter "Erlang.Otp.$Library.Beam.csproj" -File -Recurse)
    if ($projects.Count -ne 1) { throw "Expected exactly one original OTP library project: $Library" }

    return $projects[0].Directory.FullName
}

function Resolve-OriginalFixturePath {
    param([Parameter(Mandatory)][string]$Path)

    $relative = $Path.Replace('\', '/')
    if ($relative -match '^src/Erlang\.Otp\.([^/]+)\.Beam/(.+)$') {
        $libraryDirectory = Resolve-OriginalLibraryDirectory $Matches[1]
        $resolved = [IO.Path]::GetFullPath((Join-Path $libraryDirectory $Matches[2]))
        $boundary = [IO.Path]::TrimEndingDirectorySeparator($libraryDirectory) + [IO.Path]::DirectorySeparatorChar
        if (-not $resolved.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase)) { throw "Original fixture escapes its library: $Path" }

        return $resolved
    }

    return [IO.Path]::GetFullPath((Join-Path "$PSScriptRoot/.." $relative))
}
