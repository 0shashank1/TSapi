namespace TS.Application.Interfaces;

/// <summary>
/// Purpose-specific short-lived token issued after a successful
/// password unlock of a public share. Never a normal user JWT.
/// </summary>
public interface IShareAccessTokenService
{
    string Generate(Guid shareLinkId);

    /// <summary>Returns the share link id when the token is valid for it.</summary>
    Guid? Validate(string token, Guid shareLinkId);
}
