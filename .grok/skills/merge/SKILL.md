---
name: merge
description: Merge `origin/main` (or another branch given as an argument) into the current git branch, resolve merge conflicts when possible, ask the user when a resolution is ambiguous, and run the full test suite. Use when the user runs /merge, asks to merge main, pull main into the current branch, update from origin/main, or sync the current branch with another branch.
---

# Merge

Merge a source ref into the current branch, resolve conflicts, then run the full test suite.

## Arguments

- Empty / omitted: `SOURCE=origin/main`
- One token: that token is `SOURCE` (examples: `origin/main`, `main`, `origin/develop`, `feature/foo`)
- Extra tokens: stop and tell the user the skill takes at most one branch argument

Resolve `SOURCE` in this order. Use the first that `git rev-parse --verify --quiet` accepts:

1. The token as given
2. `origin/<token>` when the token has no `/`

If neither resolves after fetch (below), stop: `Cannot resolve source ref: <token>`.

## Hard stops (do these before merging)

Run from the repo root.

```powershell
git rev-parse --is-inside-work-tree
git branch --show-current
git status --porcelain
git rev-parse -q --verify MERGE_HEAD
```

Stop and ask (do not stash, commit, or reset) when any of these is true:

- Not a git work tree
- Detached HEAD (`git branch --show-current` empty)
- Working tree or index is dirty (`git status --porcelain` non-empty)
- A merge is already in progress (`MERGE_HEAD` exists) — ask continue vs `git merge --abort`
- Current branch equals `SOURCE` after resolving remotes (e.g. on `main` and `SOURCE` is local `main`). Merging `origin/main` into local `main` is allowed.

Never: force-push, rewrite history, `git reset --hard`, `git clean`, or push unless the user explicitly asks.

## Fetch then merge

If `SOURCE` is `origin/<name>` (or another configured remote), fetch that remote first:

```powershell
git fetch origin
```

Then:

```powershell
git merge <SOURCE>
```

Use a default merge (fast-forward when possible). Do not rebase. Do not add `--no-ff` or `--ff-only` unless the user asked.

- Already up to date: say so, then still run tests.
- Fast-forward or clean merge commit: go to tests.
- Conflicts: resolve below, then complete the merge.

## Resolve conflicts

List conflicts:

```powershell
git diff --name-only --diff-filter=U
```

Resolve every conflicted file. Rules:

1. **Mechanical, obvious combinations** (imports, non-overlapping edits, both sides adding distinct members): resolve yourself. Keep both sides when they do not contradict.
2. **Ambiguous logic, architecture, UI layout, or behavior**: stop and ask the user. Do not invent a design. This repo requires asking when AGENTS.md / ANTIGRAVITY.md do not specify the choice.
3. **Never auto-resolve** `AGENTS.md`, `ANTIGRAVITY.md`, or `.editorconfig`. Ask the user. Do not rewrite their rules.
4. **UI files**: `Form1.cs` layout lives in `InitializeControls()`. Do not hand-edit `Form1.Designer.cs` layout. If a UI conflict is not obvious, ask.
5. **Code edits** must follow `.editorconfig` (4-space C#, CRLF) and AGENTS.md (English comments, no new dependencies).

After each file is correct: `git add <file>`. When the index has no unmerged paths:

```powershell
git commit --no-edit
```

If the user wants to cancel mid-conflict: `git merge --abort` and stop.

If a conflict cannot be resolved without guessing and the user is unavailable, leave the merge in progress, list remaining files, and stop.

## Tests

Always run after a completed merge (including already-up-to-date):

```powershell
dotnet test --verbosity normal
```

Require 100% passing.

- Failures clearly caused by the merge (broken combination, missing using, duplicate member): fix them, then re-run tests.
- Failures that look pre-existing, architectural, or unclear: ask before changing behavior.
- Do not revert the merge because tests failed unless the user asks.

## Report

Tell the user:

- Current branch and `SOURCE`
- Fast-forward, merge commit (`<sha>` + short subject), already up to date, or aborted
- Conflicted files and how they were resolved (or that the user chose)
- Test command result (pass count / failures)

Do not push. Do not open a PR. Do not delete branches.
