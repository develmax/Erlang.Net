using System.Text;

internal static class MfaCompatibilityMarkdown
{
    public static string Generate(MfaCompatibilityLedger ledger)
    {
        var text = new StringBuilder().AppendLine(MfaCompatibilityNames.Title).AppendLine().AppendLine(MfaCompatibilityNames.Method).AppendLine()
            .AppendLine($"{MfaCompatibilityNames.BaselineLabel}: {Escape(ledger.Tag)} (`{Escape(ledger.Commit)}`).").AppendLine();
        foreach (string status in new[] { MfaCompatibilityNames.Verified, MfaCompatibilityNames.Different, MfaCompatibilityNames.Pending })
        {
            text.AppendLine("## " + status).AppendLine();
            var entries = ledger.Entries.Where(entry => entry.Status == status).ToArray();
            foreach (var module in entries.Select(entry => entry.Module).Distinct(StringComparer.Ordinal))
            {
                text.AppendLine("### " + Escape(module)).AppendLine().AppendLine(MfaCompatibilityNames.TableHeader).AppendLine(MfaCompatibilityNames.TableSeparator);
                foreach (var entry in entries.Where(entry => entry.Module == module))
                    text.AppendLine($"| `{Escape(entry.Key)}` | {Escape(entry.Status)} | {Escape(entry.Scope)} | {Evidence(entry.Evidence)} | {Escape(string.Join("; ", entry.Limits))} |");
                text.AppendLine();
            }
        }

        return text.ToString().Replace("\r\n", "\n");
    }

    public static string Escape(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string Evidence(MfaCompatibilityEvidence[] evidence) => evidence.Length == 0 ? MfaCompatibilityNames.PendingAudit : string.Join(
        "; ",
        evidence.Select(value =>
        $"[{Escape(value.Kind)}]({value.Path.Replace("\\", "/")}) `{value.Sha256}`: {Escape(value.Scope)}")
    );
}
