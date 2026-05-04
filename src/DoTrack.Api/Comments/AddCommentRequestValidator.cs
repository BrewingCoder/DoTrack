namespace DoTrack.Api.Comments;

public static class AddCommentRequestValidator
{
    private const int BodyMaxLength = 32_768;

    public static IDictionary<string, string[]>? Validate(AddCommentRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request is null)
        {
            errors["request"] = ["Request body is required."];
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            errors["body"] = ["Body is required."];
        }
        else if (request.Body.Length > BodyMaxLength)
        {
            errors["body"] = [$"Body must be at most {BodyMaxLength} characters."];
        }

        if (request.AuthorId == Guid.Empty)
        {
            errors["authorId"] = ["AuthorId is required."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
