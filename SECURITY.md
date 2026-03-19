# Security Policy

## Supported versions

The current maintenance focus is the main branch (`main`).

## Reporting a vulnerability

If you discover a security vulnerability, please:

1. **Do not** publish it directly in public issues or discussions.
2. Report it privately to the repository maintainer.
3. Include, when possible:
   - Problem description
   - Potential impact
   - Reproduction steps
   - Minimal proof of concept (if applicable)
   - Affected version/commit

We will try to:

- Confirm report receipt within a reasonable time.
- Assess severity and impact.
- Coordinate a fix and responsible disclosure.

## Security scope in this project

- API keys or secrets must not be included in source code.
- User keys must be stored securely on the device (`SecureStorage`).
- External integrations (ANU QRNG, what3words) must handle network errors, quotas, and invalid responses without exposing sensitive data.

## Best practices for contributors

- Do not commit local environment files containing sensitive data.
- Review PR changes to avoid credential leaks.
- Avoid logging tokens or secret content.

