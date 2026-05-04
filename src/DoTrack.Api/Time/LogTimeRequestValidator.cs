namespace DoTrack.Api.Time;

public static class LogTimeRequestValidator
{
    private const int DescriptionMaxLength = 2048;

    public static IDictionary<string, string[]>? Validate(LogTimeRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request is null)
        {
            errors["request"] = ["Request body is required."];
            return errors;
        }

        if (request.UserId == Guid.Empty)
        {
            errors["userId"] = ["UserId is required."];
        }

        if (request.DurationMinutes <= 0)
        {
            errors["durationMinutes"] = ["DurationMinutes must be positive."];
        }
        if (request.DurationMinutes > 24 * 60)
        {
            errors["durationMinutes"] = ["DurationMinutes cannot exceed 24 hours; split across multiple entries."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["description"] = ["Description is required for DCAA-aligned timekeeping."];
        }
        else if (request.Description.Length > DescriptionMaxLength)
        {
            errors["description"] = [$"Description must be at most {DescriptionMaxLength} characters."];
        }

        if (request.StartedAt > DateTimeOffset.UtcNow.AddDays(1))
        {
            errors["startedAt"] = ["StartedAt cannot be more than 1 day in the future."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
