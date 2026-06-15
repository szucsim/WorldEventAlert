# Function Documentation Standard

This standard applies to all future implementation changes in this repository.

## Scope

- All new public methods/functions must have XML documentation comments.
- Public constructors in service/adapter classes should include XML documentation.
- Public extension methods must include XML documentation.
- Public interface methods must include XML documentation where practical.

## Required XML Tags

- `<summary>`: one clear sentence describing intent.
- `<param>`: required for each parameter.
- `<returns>`: required for non-void methods.
- `<exception>`: required for expected thrown exceptions.

## Style Rules

- Focus on business intent and side effects, not implementation trivia.
- Mention correlation/logging behavior when relevant.
- Keep comments concise and stable under refactoring.
- Avoid restating obvious names.

## Example

```csharp
/// <summary>
/// Dispatches a notification request through the configured channel strategy and persists the attempt.
/// </summary>
/// <param name="request">The notification payload and routing metadata.</param>
/// <param name="attemptNumber">The 1-based attempt number for retry tracking.</param>
/// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
/// <returns>The persisted delivery attempt record.</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
/// <exception cref="ArgumentOutOfRangeException">Thrown when attemptNumber is less than 1.</exception>
Task<DeliveryAttempt> DispatchAsync(NotificationRequest request, int attemptNumber, CancellationToken cancellationToken = default);
```

## Process Rule

- Each implementation step markdown must include a `Function Documentation Added` section.
- PR or commit review should reject new public methods missing required XML documentation.
