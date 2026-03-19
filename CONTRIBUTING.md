# Contributing to Fatum

Thank you for your interest in contributing to Fatum.

## Project scope

Fatum is a .NET MAUI app (Android/iOS/Windows) with core logic in `FatumCommon` and UI/services in `FatumApp.Maui`.

## Development requirements

- .NET 10 SDK
- For Android: Android SDK (API 24+)
- For iOS: Xcode + Mac (Pair to Mac)
- For Windows: Windows 10/11 (SDK 19041+)

## Getting started

1. Fork the repository.
2. Create a descriptive branch:
   - `feature/change-name`
   - `fix/bug-name`
3. Restore and build:

```bash
dotnet restore
dotnet build
```

## Recommended conventions

- Keep changes small and focused.
- Avoid mixing large refactors with functional fixes in the same PR.
- Do not include secrets (API keys, tokens, credentials).
- Keep naming and style consistent with the existing codebase.
- If you add new fields to persisted models:
  - update the domain model
  - update `DatabaseInitializer` (create + migration)
  - update import/export when applicable

## Pull Requests

Include in the PR description:

- The problem being solved
- The chosen approach and tradeoffs
- How to test manually
- Screenshots/video if UI changes are included

Suggested PR checklist:

- [ ] Builds locally
- [ ] Does not add secrets or unnecessary generated files
- [ ] Updates docs (README/notes) if behavior changed
- [ ] Manual testing completed

## Sensitive areas

- KDE generation and anomaly calculations (`FatumCommon/KdeCalculator.cs`)
- SQLite persistence and migrations (`FatumCommon/Infrastructure/Data/DatabaseInitializer.cs`)
- History import/export (`FatumApp.Maui/Services/ExportService.cs`)
- External integrations (ANU QRNG, what3words)

## Bug reporting

When opening an issue, include:

- Platform (Android/iOS/Windows)
- .NET SDK version
- Reproduction steps
- Expected vs actual behavior
- Relevant logs (without sensitive data)

