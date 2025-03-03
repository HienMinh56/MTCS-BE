namespace MTCS.Data.DTOs
{
    public class TokenDTO
    {
        public required string Token { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class JWTSettings
    {
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public required string Key { get; set; }
    }
}
