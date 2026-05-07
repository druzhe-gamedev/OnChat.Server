using System.Security.Cryptography;
using System.Text;

namespace OnChat.Encryption;

public static class HashHelper
{
    private static readonly SHA256 Sha256 = SHA256.Create();

    public static string GetHash(string str, string? salt = "") =>
        BytesToString(Sha256.ComputeHash(Encoding.UTF8.GetBytes(str + salt)));

    private static string BytesToString(byte[] hashBytes)
    {
        StringBuilder hash = new();

        foreach (byte hashByte in hashBytes)
            hash.Append(hashByte.ToString("x2"));

        return hash.ToString();
    }
}