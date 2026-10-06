const fs = require('node:fs');
const path = require('node:path');

module.exports = async function publish({ github, context, directory, issueNumber }) {
  if (!Number.isSafeInteger(issueNumber) || issueNumber <= 0) {
    throw new Error('issueNumber must be a positive integer.');
  }
  const result = JSON.parse(fs.readFileSync(path.join(directory, 'result.json'), 'utf8'));
  const report = fs.readFileSync(path.join(directory, 'investigation.md'), 'utf8').trim();
  if (!['reproduced', 'not_reproduced', 'user_error', 'blocked'].includes(result.status)) {
    throw new Error('Invalid investigation status.');
  }
  if (!report || report.length > 55000 || !/^\d+\.\d+\.\d+(\.\d+)?$/.test(result.winuiex_version)) {
    throw new Error('Invalid investigation report or release version.');
  }
  for (const assessment of [result.regression, result.unpublished_fix]) {
    if (!assessment || !['confirmed', 'not_observed', 'unknown'].includes(assessment.status) ||
        typeof assessment.evidence !== 'string' || !assessment.evidence.trim()) {
      throw new Error('Regression and unpublished-fix assessments must include evidence or a blocker.');
    }
    if (result.status !== 'reproduced' && assessment.status !== 'unknown') {
      throw new Error('Comparative conclusions require a confirmed failure in the latest release.');
    }
  }
  if (result.regression.status !== 'unknown' &&
      !/^\d+\.\d+\.\d+(\.\d+)?$/.test(result.regression.baseline_version)) {
    throw new Error('The regression assessment must identify the tested baseline release.');
  }
  if (!/^[0-9a-f]{40}$/.test(result.unpublished_fix.commit)) {
    throw new Error('The unpublished-fix assessment must identify the tested source commit.');
  }
  for (const link of report.matchAll(/https:\/\/github\.com\/[^\s)<>]+\/blob\/[^\s)<>]+/g)) {
    if (!/\/blob\/[0-9a-f]{40}\/[^#]+#L\d+(-L\d+)?$/.test(link[0])) {
      throw new Error('Source citations must contain commit SHAs and line anchors.');
    }
  }
  const runUrl = `${context.serverUrl}/${context.repo.owner}/${context.repo.repo}/actions/runs/${context.runId}`;
  let attachment = '';
  if (result.status === 'reproduced') {
    if (typeof result.reproduction_evidence !== 'string' || !result.reproduction_evidence.trim()) {
      throw new Error('A reproduced outcome requires runtime evidence.');
    }
    const data = fs.readFileSync(path.join(directory, 'reproducer.zip'));
    if (data.length < 4 || data.readUInt32LE(0) !== 0x04034b50) {
      throw new Error('The source-only reproducer ZIP is missing or invalid.');
    }
    const attempt = process.env.GITHUB_RUN_ATTEMPT || '1';
    const tag = `issue-review-${issueNumber}-${context.runId}-${attempt}`;
    const { data: release } = await github.rest.repos.createRelease({
      ...context.repo,
      tag_name: tag,
      target_commitish: context.sha,
      name: `Issue #${issueNumber} reproducer (run ${context.runId}, attempt ${attempt})`,
      body: `Source-only reproducer for issue #${issueNumber}, investigated with WinUIEx ${result.winuiex_version}.\n\n[Investigation run](${runUrl})\n\nThis is an investigation attachment, not a WinUIEx package release.`,
      draft: false,
      prerelease: true,
      make_latest: 'false'
    });
    const { data: asset } = await github.rest.repos.uploadReleaseAsset({
      ...context.repo,
      release_id: release.id,
      name: `issue-${issueNumber}-reproducer.zip`,
      data,
      headers: { 'content-type': 'application/zip', 'content-length': data.length }
    });
    attachment = `\n\n**Reproducer:** [Download source-only ZIP](${asset.browser_download_url}) (persistent release asset; no binary build outputs).`;
  }
  await github.rest.issues.createComment({
    ...context.repo,
    issue_number: issueNumber,
    body: `## Copilot issue investigation\n\n${report}${attachment}\n\n---\nAutomated investigation using WinUIEx ${result.winuiex_version}. [Workflow run](${runUrl}). Findings may require maintainer confirmation.`
  });
};
