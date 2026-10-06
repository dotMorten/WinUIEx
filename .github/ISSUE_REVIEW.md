# Manual Copilot issue review

Run **Actions > Review issue with Copilot > Run workflow**, select the trusted
default branch, and supply the single input, `issue_number`.

## Setup

- Create an `issue-review` GitHub environment. Add `COPILOT_REVIEW_TOKEN` as an
  environment secret: a fine-grained personal access token with **Copilot
  Requests: Read-only** account permission belonging to an account with an active
  Copilot plan and CLI access. This workflow authenticates Copilot with that PAT
  and uses the Actions `GITHUB_TOKEN` for repository operations. Do not grant
  this PAT repository-write permissions.
- Configure environment reviewers and restrict deployment branches to the
  trusted default branch. Approve runs only after checking the issue and linked
  repro. The agent can build and execute repro code on an ephemeral hosted
  Windows runner; permission flags and prompt instructions are not a sandbox.
- Allow Actions to comment on issues. Both jobs have only `contents: read`;
  the separate publisher additionally has `issues: write` to post the
  investigation report. Checkout does not persist credentials, built-in Copilot
  MCP servers and external custom instructions are disabled, and authentication
  variables are stripped from agent shell/MCP subprocess environments.
  The investigation uses `--allow-all-tools` and `--allow-all-paths` so installed
  MSBuild/.NET/Windows SDK tools and dependency caches outside the workspace do
  not require interactive approval. This filesystem grant applies only to the
  disposable hosted runner's investigation, not to classification. URL access
  remains restricted, and the explicit `git push` and `gh` denials remain.

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
and posts the investigation report to the issue. **ZIP attachment is manual**:
the token-authenticated media upload endpoint rejects ZIP files, so the workflow
does not attempt an automatic upload. It never creates releases or tags and
does not put artifact-download links in the issue comment.

The investigation report and ZIP are retained as Actions artifacts for 90 days.
To attach a confirmed repro, download the run's `issue-review-<number>` artifact,
extract `reproducer.zip`, and drag that ZIP into a comment on the issue. This
creates a native GitHub attachment/download link. Review the generated report
and source before trusting or running it.

Each manual run adds a new report comment. For confirmed reproductions it
explicitly states that a maintainer must attach the ZIP manually, rather than
claiming an attachment was posted. Publication failures fail the workflow.

Automation lives in `workflows/review-issue.yml`, `scripts/review-issue.ps1`,
`scripts/publish-issue-review.cjs`, and `prompts/issue-review.md`, relative to
`.github`. The agent receives the actual WinUIEx clone path at runtime.

Local automation checks (mocked Copilot and GitHub calls, no publication):

```powershell
pwsh -NoProfile -File .github\scripts\tests\review-issue.tests.ps1
node --test .github\scripts\tests\publish-issue-review.test.cjs
```
