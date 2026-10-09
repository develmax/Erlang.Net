# CHANGELOG entry template

Append an entry to the root CHANGELOG.md after the previous part. Write every entry in English. Replace placeholders with actual facts and results.

```markdown
## Part NNN — YYYY-MM-DD — Concrete result

**Part status:** completed / partially implemented / blocked.
**Objective and scope:** requirement, initial behavior and boundaries of this part.

### Changes

- What changed and why; concrete before/after behavior.
- Affected projects/files with links.
- Fixed defects and added permanent regression tests.

### Decisions and compatibility

- Inspected reference sources and pinned OTP version.
- Reuse/dependency/license decisions or links to the current audit.
- Supported language constructs/MFAs/OTP contracts and their statuses.
- Known differences, risks, limitations and unfinished scope.

### Validation

| Check | Exact command and saved report | Actual result |
| --- | --- | --- |
| Build/tests/integration/oracle | ... | ... |

List unrun checks and explain why. Label historical reports as historical;
do not present them as checks executed in this part.

### Handoff

- Unfinished tasks, blockers and required actions.
- Next executable step.
- Updated PROGRESS/COMPATIBILITY/semantic differences/NEXT_STEPS.
- Local checkpoint commit title; commit hash may be obtained from Git history.
- Push/publication: record only actual authorized actions, if any.
```
