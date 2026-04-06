namespace SecureVault.Domain.Errors;

public static class DomainErrors
{
    public static class User
    {
        public const string NotFound = "user.not_found";
        public const string EmailAlreadyExists = "user.email_already_exists";
        public const string InvalidCredentials = "user.invalid_credentials";
    }

    public static class Credential
    {
        public const string NotFound = "credential.not_found";
        public const string Forbidden = "credential.forbidden";
    }

    public static class Token
    {
        public const string Invalid = "token.invalid";
        public const string Expired = "token.expired";
        public const string Revoked = "token.revoked";
    }
}
