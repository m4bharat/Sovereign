namespace Sovereign.Application.DTOs
{
    public class CreateRelationshipRequest
    {
        public string ContactId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
    }
}
