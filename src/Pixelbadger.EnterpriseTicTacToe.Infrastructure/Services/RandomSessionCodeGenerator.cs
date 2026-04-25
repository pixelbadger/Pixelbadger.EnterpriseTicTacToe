using System.Security.Cryptography;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class RandomSessionCodeGenerator : ISessionCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string GenerateCode()
    {
        Span<char> output = stackalloc char[GameSession.SessionCodeLength];
        Span<byte> bytes = stackalloc byte[GameSession.SessionCodeLength];
        RandomNumberGenerator.Fill(bytes);

        for (var index = 0; index < output.Length; index++)
        {
            output[index] = Alphabet[bytes[index] % Alphabet.Length];
        }

        return new string(output);
    }
}
