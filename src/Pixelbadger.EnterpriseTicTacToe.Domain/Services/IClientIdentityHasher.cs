namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface IClientIdentityHasher
{
    string Hash(string clientIdentity);
}
