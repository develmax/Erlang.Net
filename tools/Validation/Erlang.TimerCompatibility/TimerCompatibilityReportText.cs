internal static class TimerCompatibilityReportText
{
    internal static string Upstream(string backend, int passed, int cases)
        => $"{backend}: upstream {passed}/{cases}";

    internal static string Primary(
        string backend,
        int passed,
        int cases,
        int effects,
        int failures
    )
        => $"{backend}: {passed}/{cases} contracts; {effects} scenarios; {failures} failures";
}
