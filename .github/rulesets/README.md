# Branch rulesets

`main-branch.json` is the ruleset protecting `main`, kept in the repo so the branch policy is
reviewable and restorable rather than living only in the GitHub UI.

## Applying it

**Settings → Rules → Rulesets → New ruleset → Import a ruleset**, then upload
`main-branch.json`. Or via the CLI:

```bash
gh api repos/bcolemutech/dol-con-v2/rulesets --method POST --input .github/rulesets/main-branch.json
```

To update an existing ruleset in place, find its id with
`gh api repos/bcolemutech/dol-con-v2/rulesets` and `PUT` to `/rulesets/{id}` with the same file.

## Why these settings

The repo is a solo project where most changes arrive as pull requests opened by an agent
(Claude Code). That shapes two choices that would look wrong on a team repo.

| Rule | Setting | Reason |
| --- | --- | --- |
| Require a pull request | **on**, `0` approvals required | Forces every change onto a branch with CI, so nothing lands on `main` unreviewed by the test suite. **Approvals are 0 deliberately** — on a solo repo GitHub will not let you approve your own PR, so requiring ≥1 approval makes every PR permanently unmergeable without a bypass. The PR is the review surface; you read the diff and click merge. |
| Allowed merge methods | **squash only** | Matches the existing history (one commit per PR) and keeps `required_linear_history` satisfiable. |
| Require status checks | **on**, strict | All eight checks must pass. `strict` also requires the branch to be up to date with `main` first, which catches semantic conflicts that merge cleanly but break the build. |
| Require conversation resolution | **on** | Carried over from the previous classic branch protection. Review comments must be resolved before merge. |
| Require linear history | **on** | Pairs with squash-only merges. Keeps `git log` readable and bisectable. |
| Block force pushes | **on** | Prevents accidental history loss — but see bypass below. |
| Restrict deletions | **on** | `main` cannot be deleted. |
| Bypass actors | **repository admin, always** | The important improvement over classic branch protection. Deliberate history maintenance (e.g. purging a large blob with `git filter-repo`) needs one force push; with classic protection that meant *disabling protection, pushing, re-enabling* and hoping nothing landed in the gap. With an admin bypass the rules stay on permanently and the rare intentional force push just works. |

### Status check names

The `context` values must match the **job names** in `.github/workflows/`, not the workflow names.
If you rename a job, update this file — a required check that never reports will block every merge.

Current checks come from `dotnet-test.yml` (`Test Core on <os>`, `Build MonoGame on <os>`,
`Verify world bake is reproducible`) and `version-check.yml` (`Check Version Bump`).

`Verify world bake is reproducible` is load-bearing: `world.dol` is a gitignored build artifact
rather than a committed file, which is only safe while the bake is byte-reproducible. See
`docs/WORLD_DOL_FORMAT.md`.

## Notes for agent-driven work

- The agent branches, pushes, and opens a PR; it never pushes to `main`. These rules enforce that
  rather than relying on the agent to remember.
- With `0` required approvals the agent *can* merge its own PR once checks pass. If you would rather
  be the only one who merges, the cleanest lever is a GitHub Actions/token permission change, not an
  approval count — raising the count to 1 blocks you as well.
