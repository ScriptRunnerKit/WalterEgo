# Contributing to WalterEgo

Thank you for your interest in WalterEgo! This guide explains how to propose changes.

This project follows the [ScriptRunnerKit Code of Conduct](https://github.com/ScriptRunnerKit/.github/blob/main/CODE_OF_CONDUCT.md).

## Before you start

- **Open an issue first** for a new feature or a change in behavior, so we can agree on the scope before any code is
  written. Bug fixes with a clear reproduction can go straight to a pull request.
- **Security issues never go in public issues.** See [SECURITY.md](SECURITY.md).
- WalterEgo is **exclusively defensive**. Contributions that attack, exploit or track the source of a trip ("hack-back")
  are not accepted.
- **New dependencies** must be discussed in the issue first: supply chain matters in a security project.

## Building and testing

You need exactly the .NET SDK version pinned in [`global.json`](global.json): the package lock files depend on it, and
the CI restores in locked mode. Tests marked with the `Category=Docker` trait also need Docker.

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

The build treats warnings as errors and runs the .NET analyzers, the code style rules, the public API analyzer and the
banned API analyzer.

## Pull requests

- The **title** follows [Conventional Commits](https://www.conventionalcommits.org/), for example
  `feat(aspnetcore): add honey endpoint sensor`. Pull requests are squash-merged and the title becomes the commit
  message.
- The **description** explains what changed, how it was tested and the impact on the public API.
- A change to a **contract** — the public API, the alert event, the configuration or the CLI — updates the affected
  [samples](samples) in the same pull request.
- Public API changes are recorded in the project's `PublicAPI.Unshipped.txt`.
- User-visible changes get an entry in the `[Unreleased]` section of [CHANGELOG.md](CHANGELOG.md).
- Tests and samples use clearly fictitious values. Never commit real secrets.

## Code style

- Code, identifiers and XML documentation are in English.
- Every public type and member has complete XML documentation.
- Comments explain the non-obvious *why*; they do not repeat what the code says.
- Test names describe the behavior they check.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
