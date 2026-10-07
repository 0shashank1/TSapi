using System.Security.Cryptography;
using System.Text;

namespace TS.Application.Interfaces;

public interface IRefreshTokenGenerator
{
    string Generate();

    byte[] Hash(string token);
}
