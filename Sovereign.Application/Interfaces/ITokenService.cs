using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface ITokenService
{
    string Create(UserAccount user);
}
