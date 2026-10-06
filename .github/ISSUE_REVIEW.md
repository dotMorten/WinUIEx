# Manual Copilot issue review

Run **Actions > Review issue with Copilot > Run workflow**, select the trusted
default branch, and supply the single input, `issue_number`.

## Setup

- Create an `issue-review` GitHub environment. Add `COPILOT_REVIEW_TOKEN` as an
  environment secret: a fine-grained personal access token with **Copilot
  Requests** permission belonging to an account with an active Copilot plan and
  CLI access. The Actions `GITHUB_TOKEN` cannot authenticate Copilot. Do not
  grant this PAT repository-write permissions.
- Configure environment reviewers and restrict deployment branches to the
  trusted default branch. Approve runs only after checking the issue and linked
  repro. The agent can build and execute repro code on an ephemeral hosted
  Windows runner; permission flags and prompt instructions are not a sandbox.
- Allow Actions to create releases and comment on issues. The investigation job
  has only read permissions; only the separate publisher has `contents: write`
  and `issues: write`. Checkout does not persist credentials, built-in Copilot
  MCP servers and external custom instructions are disabled, and authentication
  variables are stripped from agent shell/MCP subprocess environments.

Each run installs the latest Copilot CLI, **WinApp CLI** (with
`microsoft/setup-WinAppCli`), Node.js, and stable .NET SDK on `windows-latest`,
and uses the runner's MSBuild. Actions follow their upstream `main` branches
rather than version tags.
It also installs the trusted WinUI development skills and `winui:winui-dev`
agent from the current default branch of `microsoft/win-dev-skills`, with:

```powershell
copilot plugin marketplace add microsoft/win-dev-skills
copilot plugin install winui@win-dev-skills
```

The investigation runs with that agent and can load its design, build/run, and
UI-testing skills. Copilot configuration is isolated in the runner's temporary
directory; no repro-provided plugins or instructions are trusted.
Copilot usage is billed to the token owner's plan.

## Behavior and outputs

The description is classified first. Non-bug reports stop without cloning a
repro or commenting. Suspected bugs proceed even when user error is possible.
The agent clones the author's public repro when available, creates a minimal
standalone repro using the latest stable **NuGet** WinUIEx release, examines the
WinUIEx checkout and optionally the WinUI framework source, and revisits the
repro to remove unrelated code. Source citations must use full commit SHAs and
line anchors.
The source checkout uses the current default branch, independent of the selected
workflow ref. The agent tests a relevant older release to assess regressions and
compares the released failure with a locally built current-source package to
check whether the fix is already present but unshipped. Comparisons use separate
repro copies; the published repro uses the latest released package resolved
for that run. Its concrete tested version is recorded for reproducibility,
not hardcoded into the automation.
The report identifies tested versions/commits and explicitly marks inconclusive
comparisons rather than inferring a regression or fix from source inspection.
The prompt directs the agent to use WinApp CLI for minimal project scaffolding,
project-mode build/deployment/launch (`winapp run`), and focused UI automation
(`winapp ui`), retaining the launched app's PID and checking actual behavior.

Confirmed failures must be observed at runtime in the final simplified repro.
Build success alone is not reproduction. Hosted runners may lack the desktop
interaction needed for UI bugs; those investigations must report **blocked**,
not claim success or absence of a bug. Missing/private repros and incomplete
investigations are documented explicitly. Bug investigations post their report,
including not-reproduced, user-error, and blocked outcomes.

For a confirmed reproduction, the workflow creates a source-only ZIP (excluding
`bin`, `obj`, version-control folders, caches, binaries, and signing keys),
uploads it to a dedicated **prerelease**, and links the persistent asset in the
issue comment. GitHub has no supported REST API for directly attaching a ZIP to
an issue comment; the release link is the attachment delivery mechanism. These
releases are marked `make_latest: false` and are not WinUIEx package releases.
The investigation report and ZIP are also retained as Actions artifacts for
90 days. Review the generated report and source before trusting or running it.

Each manual run adds a new comment and, if reproduced, a run/attempt-specific
release. Publication failures fail the workflow rather than claiming a comment
or attachment was posted. If release creation/upload succeeds but commenting
fails, the release remains available from the repository's releases page.

Automation lives in `workflows/review-issue.yml`, `scripts/review-issue.ps1`,
`scripts/publish-issue-review.cjs`, and `prompts/issue-review.md`, relative to
`.github`. The agent receives the actual WinUIEx clone path at runtime.

Local automation checks (mocked Copilot and GitHub calls, no publication):

```powershell
pwsh -NoProfile -File .github\scripts\tests\review-issue.tests.ps1
node --test .github\scripts\tests\publish-issue-review.test.cjs
```
