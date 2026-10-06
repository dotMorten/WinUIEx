const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { test } = require('node:test');
const publish = require('../publish-issue-review.cjs');

function fixture(t, status = 'reproduced') {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'issue-review-publish-test-'));
  t.after(() => fs.rmSync(directory, { recursive: true }));
  const result = {
    status,
    winuiex_version: '2.9.0',
    reproduction_evidence: status === 'reproduced' ? 'Observed the reported failure after invoking the control.' : '',
    regression: { status: 'unknown', baseline_version: null, evidence: 'Baseline cannot be exercised.' },
    unpublished_fix: { status: 'unknown', commit: 'a'.repeat(40), evidence: 'HEAD cannot be exercised.' }
  };
  const save = () => fs.writeFileSync(path.join(directory, 'result.json'), JSON.stringify(result));
  save();
  fs.writeFileSync(path.join(directory, 'investigation.md'), '**Outcome:** ' + status);
  fs.writeFileSync(path.join(directory, 'reproducer.zip'), Buffer.from([0x50, 0x4b, 0x03, 0x04, 0x00]));
  const calls = [];
  const github = {
    rest: {
      repos: {
        createRelease: async args => {
          calls.push(['release', args]);
          return { data: { id: 42 } };
        },
        uploadReleaseAsset: async args => {
          calls.push(['asset', args]);
          return { data: { browser_download_url: 'https://github.com/owner/repo/releases/download/tag/reproducer.zip' } };
        }
      },
      issues: {
        createComment: async args => {
          calls.push(['comment', args]);
          return { data: {} };
        }
      }
    }
  };
  const context = {
    repo: { owner: 'owner', repo: 'repo' },
    sha: 'b'.repeat(40), serverUrl: 'https://github.com', runId: 123
  };
  return { directory, result, save, calls, github, context, issueNumber: 7 };
}

test('confirmed reproduction publishes a non-latest prerelease and persistent attachment comment', async t => {
  const f = fixture(t);
  f.result.regression = { status: 'confirmed', baseline_version: '2.8.0', evidence: 'Older version passes; latest fails.' };
  f.result.unpublished_fix.status = 'confirmed';
  f.result.unpublished_fix.evidence = 'Current source build passes the same test.';
  f.save();
  await publish(f);
  assert.deepEqual(f.calls.map(call => call[0]), ['release', 'asset', 'comment']);
  assert.equal(f.calls[0][1].prerelease, true);
  assert.equal(f.calls[0][1].make_latest, 'false');
  assert.equal(f.calls[0][1].target_commitish, f.context.sha);
  assert.equal(f.calls[1][1].name, 'issue-7-reproducer.zip');
  assert.equal(f.calls[1][1].headers['content-type'], 'application/zip');
  assert.match(f.calls[2][1].body, /Download source-only ZIP/);
  assert.equal(f.calls[2][1].issue_number, 7);
});

for (const status of ['blocked', 'not_reproduced', 'user_error']) {
  test(`${status} comments honestly without creating a release`, async t => {
    const f = fixture(t, status);
    fs.unlinkSync(path.join(f.directory, 'reproducer.zip'));
    await publish(f);
    assert.deepEqual(f.calls.map(call => call[0]), ['comment']);
    assert.match(f.calls[0][1].body, new RegExp(status));
    assert.doesNotMatch(f.calls[0][1].body, /Download source-only ZIP/);
  });
}

for (const status of ['confirmed', 'not_observed']) {
  test(`rejects ${status} comparisons when runtime reproduction was blocked`, async t => {
    const f = fixture(t, 'blocked');
    f.result.unpublished_fix.status = status;
    f.save();
    await assert.rejects(publish(f), /confirmed failure/);
    assert.equal(f.calls.length, 0);
  });
}

test('rejects regression conclusions without a baseline version', async t => {
  const f = fixture(t);
  f.result.regression.status = 'confirmed';
  f.save();
  await assert.rejects(publish(f), /baseline release/);
  assert.equal(f.calls.length, 0);
});

test('rejects reproduction without observed evidence', async t => {
  const f = fixture(t);
  f.result.reproduction_evidence = '';
  f.save();
  await assert.rejects(publish(f), /runtime evidence/);
  assert.equal(f.calls.length, 0);
});

test('rejects missing ZIP before any publication', async t => {
  const f = fixture(t);
  fs.unlinkSync(path.join(f.directory, 'reproducer.zip'));
  await assert.rejects(publish(f), /ENOENT/);
  assert.equal(f.calls.length, 0);
});

test('rejects branch-based source citations', async t => {
  const f = fixture(t);
  fs.writeFileSync(path.join(f.directory, 'investigation.md'), '[source](https://github.com/owner/repo/blob/main/src/App.cs#L10)');
  await assert.rejects(publish(f), /commit SHAs/);
  assert.equal(f.calls.length, 0);
});

test('accepts commit- and line-pinned citations', async t => {
  const f = fixture(t);
  fs.writeFileSync(path.join(f.directory, 'investigation.md'), `[source](https://github.com/owner/repo/blob/${'a'.repeat(40)}/src/App.cs#L10-L20)`);
  await publish(f);
  assert.equal(f.calls.length, 3);
});

test('rejects invalid issue numbers and statuses', async t => {
  const f = fixture(t);
  await assert.rejects(publish({ ...f, issueNumber: 1.5 }), /positive integer/);
  f.result.status = 'success';
  f.save();
  await assert.rejects(publish(f), /Invalid investigation status/);
  assert.equal(f.calls.length, 0);
});

test('upload failure prevents a success-shaped issue comment', async t => {
  const f = fixture(t);
  f.github.rest.repos.uploadReleaseAsset = async () => { throw new Error('Upload failed'); };
  await assert.rejects(publish(f), /Upload failed/);
  assert.deepEqual(f.calls.map(call => call[0]), ['release']);
});
