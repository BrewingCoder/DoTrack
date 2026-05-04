namespace DoTrack.Api.WorkItems;

public static class UpdateWorkItemRequestValidator
{
    private const int TitleMaxLength = 512;

    public static IDictionary<string, string[]>? Validate(UpdateWorkItemRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request is null)
        {
            errors["request"] = ["Request body is required."];
            return errors;
        }

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                errors["title"] = ["Title cannot be blank."];
            }
            else if (request.Title.Length > TitleMaxLength)
            {
                errors["title"] = [$"Title must be at most {TitleMaxLength} characters."];
            }
        }

        if (request.EstimatePoints is < 0)
        {
            errors["estimatePoints"] = ["EstimatePoints must be non-negative."];
        }

        if (request.AssigneeId is { } assignee && assignee == Guid.Empty)
        {
            errors["assigneeId"] = ["AssigneeId cannot be Guid.Empty; omit the field to leave unchanged."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
