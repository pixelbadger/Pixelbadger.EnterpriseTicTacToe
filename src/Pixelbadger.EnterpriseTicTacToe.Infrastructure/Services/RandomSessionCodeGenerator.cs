using System.Security.Cryptography;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class RandomSessionCodeGenerator : SessionCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string GenerateCode()
    {
        Span<char> output = stackalloc char[6];
        Span<byte> bytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(bytes);

        for (var index = 0; index < output.Length; index++)
        {
            output[index] = Alphabet[bytes[index] % Alphabet.Length];
        }

        return new string(output);
    }
}
