using System.Globalization;

internal static class ReadinessText
{
    public const string Command = "readiness";
    public const string InventoryFile = "otp-inventory.json";
    public const string RegistryFile = "supported-mfas.json";
    public const string ScopeFile = "module-readiness-scope.json";
    public const string BaselineFile = "reference-baseline.json";
    public const string ReportJson = "module-readiness.json";
    public const string ReportMarkdown = "MODULE_READINESS.md";
    public const string Partial = "Partially compatible";
    public const string NotStarted = "Not started";
    public const string Passed = "Passed";
    public const string MfaTestPrefix = "mfa/";
    public const string Title = "# Module readiness";
    public const string Method = "API presence is the registered, directly tested MFA count divided by the union of pinned explicit exports and BIF declarations. It is not semantic completion or test coverage. Preprocessing, generated/NIF/conditional/platform exports and complete contracts remain unresolved. Component milestones have no invented weighted percentage.";
    public const string ComponentHeading = "## C# components";
    public const string ComponentHeader = "| Component | Status | Related passed tests | Delivered | Next milestone |";
    public const string ComponentSeparator = "| --- | --- | ---: | --- | --- |";
    public const string ModuleHeading = "## Erlang API modules";
    public const string ModuleHeader = "| Module | Registered MFAs | Change | Reference declarations | API presence | Status |";
    public const string ModuleSeparator = "| --- | ---: | ---: | ---: | ---: | --- |";
    public const string FacadeBoundary = "gen_server and supervisor have C# host facades and selected registered Erlang-module adapters; full behaviour contracts remain partial. Full zero-registration inventory rows and implemented MFA names are preserved in module-readiness.json. Related test counts can overlap components and are not summed as independent coverage.";

    public static string Summary(
        string tag,
        int passed,
        int registered,
        int previous,
        int implementedModules,
        int inventoryRows
    ) => $"Baseline **{tag}**. Local tests **{passed} passed**. Runtime MFAs **{registered}** (previous snapshot **{previous}**, delta **{registered - previous:+0;-0;0}**), in **{implementedModules}** modules. Reference inventory: **{inventoryRows}** source-module rows.";

    public static string ComponentRow(
        string id,
        string status,
        int tests,
        string[] implemented,
        string[] remaining
    ) => $"| {id} | {status} | {tests} | {string.Join("; ", implemented)} | {string.Join("; ", remaining)} |";

    public static string ModuleRow(
        string name,
        int registered,
        int delta,
        int reference,
        decimal? percent,
        string status
    ) => $"| {name} | {registered} | {delta:+0;-0;0} | {reference} | {(percent is null ? "n/a" : percent.Value.ToString("0.0", CultureInfo.InvariantCulture) + "%")} | {status} |";
}
