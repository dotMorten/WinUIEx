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

test('confirmed reproduction posts only the report and requests a manual ZIP attachment', async t => {
  const f = fixture(t);
  f.result.regression = { status: 'confirmed', baseline_version: '2.8.0', evidence: 'Older version passes; latest fails.' };
  f.result.unpublished_fix.status = 'confirmed';
  f.result.unpublished_fix.evidence = 'Current source build passes the same test.';
  f.save();
  await publish(f);
  assert.deepEqual(f.calls.map(call => call[0]), ['comment']);
  assert.match(f.calls[0][1].body, /not attached automatically/);
  assert.match(f.calls[0][1].body, /maintainer must upload/);
  assert.doesNotMatch(f.calls[0][1].body, /user-attachments|releases\/|actions\/runs\/123\/artifacts/);
  assert.equal(f.calls[0][1].issue_number, 7);
});

for (const status of ['blocked', 'not_reproduced', 'user_error']) {
  test(`${status} comments honestly without creating a release`, async t => {
    const f = fixture(t, status);
    fs.unlinkSync(path.join(f.directory, 'reproducer.zip'));
    await publish(f);
    assert.deepEqual(f.calls.map(call => call[0]), ['comment']);
    assert.match(f.calls[0][1].body, new RegExp(status));
    assert.doesNotMatch(f.calls[0][1].body, /user-attachments/);
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
  assert.deepEqual(f.calls.map(call => call[0]), ['comment']);
});

test('rejects invalid issue numbers and statuses', async t => {
  const f = fixture(t);
  await assert.rejects(publish({ ...f, issueNumber: 1.5 }), /positive integer/);
  f.result.status = 'success';
  f.save();
  await assert.rejects(publish(f), /Invalid investigation status/);
  assert.equal(f.calls.length, 0);
});

test('comment publication failures are surfaced', async t => {
  const f = fixture(t);
  f.github.rest.issues.createComment = async () => { throw new Error('Comment failed'); };
  await assert.rejects(publish(f), /Comment failed/);
  assert.equal(f.calls.length, 0);
});

test('publisher has no repository-write permission', () => {
  const workflow = fs.readFileSync(path.join(__dirname, '..', '..', 'workflows', 'review-issue.yml'), 'utf8');
  const publisher = workflow.slice(workflow.indexOf('\n  publish:'));
  assert.match(publisher, /contents: read/);
  assert.match(publisher, /issues: write/);
  assert.doesNotMatch(publisher, /contents: write/);
});
