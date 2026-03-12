# Contributing to Streamarr

## Reporting Bugs

Open a [GitHub Issue](https://github.com/nardokor/streamarr/issues) with as much detail as possible: steps to reproduce, expected vs actual behaviour, and your platform/version.

## Suggesting Features

Open an issue describing the feature and why it would be useful. Check existing issues first to avoid duplicates.

## Development Setup

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20+
- [Yarn](https://yarnpkg.com/)
- [Git](https://git-scm.com/)

### Getting started

```bash
git clone https://github.com/nardokor/streamarr.git
cd streamarr

# Frontend
yarn install
yarn start   # webpack dev server with hot reload

# Backend (separate terminal)
dotnet run --project src/Streamarr.Console -- --nobrowser --data=/tmp/streamarr-dev
```

Open [http://localhost:8990](http://localhost:8990). The backend serves the frontend in production; during development `yarn start` proxies API calls to the backend.

### Running tests

```bash
dotnet test src/Streamarr.sln
```

## Submitting a Pull Request

- Branch from `main` with a descriptive name (`fix-ytdlp-update`, `feat-twitch-metadata`)
- Keep PRs focused — one feature or fix per PR
- Add or update tests where relevant
- PRs target `main`

## Code Style

- Backend: C# with StyleCop (enforced by the build)
- Frontend: TypeScript, ESLint/Prettier via existing config
- Follow the patterns already in the codebase
