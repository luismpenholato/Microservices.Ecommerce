# Security Policy

## Supported versions

This is a portfolio / reference project. Security fixes are applied on a best-effort basis to the default branch.

| Version / branch | Supported |
| ---------------- | --------- |
| `main` (latest)  | Yes       |
| Older tags       | No        |

## Reporting a vulnerability

If you discover a security vulnerability, please **do not** open a public GitHub issue.

Instead, report it privately through one of the following:

- [GitHub private vulnerability reporting](https://github.com/luismpenholato/Microservices.Ecommerce/security/advisories/new) (preferred)
- Or contact the repository owner via GitHub profile: [luismpenholato](https://github.com/luismpenholato)

Include:

- A clear description of the issue
- Steps to reproduce
- Potential impact
- Suggested fix (if available)

## Security considerations for local demo use

This repository is intended for **local development and learning**, not production deployment as-is.

- Demo JWT shared secrets and credentials are for Docker Compose only.
- Do not expose broker, database or observability ports to untrusted networks.
- Review [docs/security.md](docs/security.md) before adapting the design.

## Out of scope

General feature requests and non-security bugs should be reported via [GitHub Issues](https://github.com/luismpenholato/Microservices.Ecommerce/issues).
