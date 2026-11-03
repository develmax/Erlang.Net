# Pinned baseline and immutable terms

## Status and current interpretation

The baseline and term contracts remain governed by the current project instructions. This historical record does not upgrade the OTP baseline or declare semantic completion.

The owning current rules are [AGENTS.md](../../AGENTS.md); current acceptance is recorded in the [changelog](../../CHANGELOG.md) and [remaining work](../remaining-work/INDEX.md).

## Context and original decision

1. Pin stable OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The existing reference HEAD is newer and is not the baseline.

2. Pin .NET SDK 10.0.400 and C# 14, net10.0. Avoid external restore dependencies in the initial runtime/test bootstrap.

3. Use distinct term classes, explicit exact/numeric comparison, immutable cons cells/maps/buffers and Erlang exceptions. No object/null-based term contract.

## Consequences and evidence

Read these original statements in their historical checkpoint context. Preserve their limits instead of inferring current completion. [Complete source record](../history/decisions/009.md) and [all decision history](../history/decisions/index.md).
