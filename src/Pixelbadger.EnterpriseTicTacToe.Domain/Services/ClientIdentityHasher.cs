namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface ClientIdentityHasher
{
    string Hash(string clientIdentity);
}
