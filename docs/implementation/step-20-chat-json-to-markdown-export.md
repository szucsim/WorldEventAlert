# Step 20 - Chat JSON to Markdown Export

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Add a script that converts the exported Copilot chat JSON into a reviewer-friendly markdown transcript.
- Include guidance for native VS Code chat import using `Chat: Import Chat...`.

## Delivered

- Added PowerShell export script:
  - `docs/copilot-chat/json-to-md.ps1`
- Script behavior:
  - reads `docs/copilot-chat/chat.json` by default
  - writes `docs/copilot-chat/chat.md` by default
  - preserves user and assistant turn structure in markdown
  - excludes internal thinking blocks
  - supports optional inclusion of tool invocation lines via `-IncludeToolEvents`
- Added native import guidance block at top of generated markdown.
- Generated markdown transcript artifact:
  - `docs/copilot-chat/chat.md`

## Function Documentation Added

- Not applicable for this step (PowerShell utility script; no C# public API surface changed).

## Logging Added

- No new application logging events were introduced in this utility step.

## Validation

- Ran: `./docs/copilot-chat/json-to-md.ps1`
- Result: markdown export created successfully at `docs/copilot-chat/chat.md`.

## Notes

- The JSON source remains the authoritative artifact for native replay/import in VS Code.