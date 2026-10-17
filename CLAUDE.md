# CLAUDE.md

## Git Commit Rules

- Commit after every logical, self-contained change. Do not batch unrelated changes into one commit.
- Use Conventional Commits format for the subject line: `type(scope): summary`
  - Types: `feat`, `fix`, `refactor`, `perf`, `style`, `docs`, `test`, `build`, `ci`, `chore`, `revert`
  - Scope is the affected area (e.g. `history`, `theme`, `toolbar`, `graph`, `ui`).
  - Use `!` after the type/scope for breaking changes (e.g. `feat!: ...`).
- Subject line only: no body, no description, no footer.
- No attribution of any kind: no `Co-Authored-By`, no "Generated with Claude Code", no AI signatures.
- Write the summary in lowercase imperative mood, under ~72 characters, with no trailing period.

## Avoid Ambiguous Commits

- The subject must state exactly what changed and where. Someone reading only `git log --oneline` should understand the change.
- Never use vague messages such as `update`, `fix`, `changes`, `wip`, `misc`, `cleanup`, `fix bug`, `update files`, or `minor tweaks`.
- Name the concrete behavior or component, e.g. `fix(history): keep selection when refreshing the commit list`, not `fix(history): fix issue`.
- One purpose per commit. If the summary needs "and" to join unrelated changes, split it into separate commits.
- Stage only the files that belong to the change; review `git diff --staged` before committing.
