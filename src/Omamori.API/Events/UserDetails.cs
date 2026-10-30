namespace Omamori.API
{
    public sealed record UserDetails
    {
        public bool LoggedIn { get; init; }

        /// <summary>null when not logged in</summary>
        public int? Id { get; init; }

        public string Username { get; init; } = string.Empty;

        /// <summary>two letters like JP, empty when unknown</summary>
        public string Country { get; init; } = string.Empty;
    }
}
