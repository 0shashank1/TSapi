namespace TS.Application.Interfaces;

public interface IShareCodeService
{
    /// <summary>Generates a new random human-typable share code.</summary>
    string GenerateCode();

    /// <summary>HMAC-SHA256 of the code under the given key version.</summary>
    byte[] HashCode(string code, short keyVersion);

    /// <summary>Key version used for newly created links.</summary>
    short CurrentKeyVersion { get; }

    /// <summary>All configured key versions, newest first.</summary>
    IReadOnlyList<short> KeyVersions { get; }
}
