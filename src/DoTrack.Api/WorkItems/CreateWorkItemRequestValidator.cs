using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.WorkItems;

public static class CreateWorkItemRequestValidator
{
    private const int TitleMaxLength = 512;

    public static IDictionary<string, string[]>? Validate(CreateWorkItemRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request is null)
        {
            errors["request"] = ["Request body is required."];
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Length > TitleMaxLength)
        {
            errors["title"] = [$"Title must be at most {TitleMaxLength} characters."];
        }

        if (request.Tier == WorkItemTier.Item && request.Type is null)
        {
            errors["type"] = ["Type is required when tier is Item."];
        }

        if (request.EstimatePoints is < 0)
        {
            errors["estimatePoints"] = ["EstimatePoints must be non-negative."];
        }

        if (request.ReporterId == Guid.Empty)
        {
            errors["reporterId"] = ["ReporterId is required."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
