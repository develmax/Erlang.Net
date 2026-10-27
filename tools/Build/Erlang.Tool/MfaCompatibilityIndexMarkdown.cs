using System.Globalization;
using System.Text;

internal static class MfaCompatibilityIndexMarkdown
{
    public static string Generate(MfaCompatibilityLedger ledger)
    {
        int Count(string status) => ledger.Entries.Count(entry => entry.Status == status);
        var text = new StringBuilder().AppendLine(MfaCompatibilityIndexNames.Title).AppendLine()
            .AppendLine($"{MfaCompatibilityIndexNames.Baseline}: **{MfaCompatibilityMarkdown.Escape(ledger.Tag)}** (`{MfaCompatibilityMarkdown.Escape(ledger.Commit)}`).").AppendLine()
            .AppendLine(MfaCompatibilityIndexNames.Introduction).AppendLine()
            .AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                MfaCompatibilityIndexNames.Summary,
                Count(MfaCompatibilityNames.Verified),
                Count(MfaCompatibilityNames.Different),
                Count(MfaCompatibilityNames.Pending)
            )).AppendLine()
            .AppendLine(MfaCompatibilityIndexNames.Scope).AppendLine();
        foreach (string status in new[] { MfaCompatibilityNames.Verified, MfaCompatibilityNames.Different, MfaCompatibilityNames.Pending })
        {
            MfaCompatibilityEntry[] entries = ledger.Entries.Where(entry => entry.Status == status)
                .OrderBy(entry => entry.Module, StringComparer.Ordinal).ThenBy(entry => entry.Function, StringComparer.Ordinal)
                .ThenBy(entry => entry.Arity).ToArray();
            string anchor = status switch
            {
                MfaCompatibilityNames.Verified => MfaCompatibilityIndexNames.VerifiedAnchor,
                MfaCompatibilityNames.Different => MfaCompatibilityIndexNames.DifferentAnchor,
                _ => MfaCompatibilityIndexNames.PendingAnchor
            };
            text.AppendLine($"## {MfaCompatibilityNames.DisplayStatus(status)} ({entries.Length})").AppendLine()
                .AppendLine(MfaCompatibilityIndexNames.TableHeader).AppendLine(MfaCompatibilityIndexNames.TableSeparator);
            foreach (MfaCompatibilityEntry entry in entries)
                text.AppendLine($"| {MfaCompatibilityMarkdown.Code(entry.Key)} | [{MfaCompatibilityIndexNames.Details}]({MfaCompatibilityNames.Report}#{anchor}) |");
            text.AppendLine();
        }

        return text.ToString().Replace("\r\n", "\n").TrimEnd('\n') + "\n";
    }
}
