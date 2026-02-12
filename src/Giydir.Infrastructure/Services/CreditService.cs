using Giydir.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.Services;

public class CreditService : ICreditService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreditService> _logger;

    public CreditService(IUserRepository userRepository, ILogger<CreditService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<CreditCheckResult> CheckCreditsAsync(int userId, int required)
    {
        var credits = await _userRepository.GetCreditsAsync(userId);

        var result = new CreditCheckResult
        {
            HasEnough = credits >= required,
            CurrentCredits = credits,
            Required = required
        };

        if (!result.HasEnough)
        {
            _logger.LogWarning("Yetersiz kredi: UserId={UserId}, Mevcut={Current}, Gerekli={Required}",
                userId, credits, required);
        }

        return result;
    }

    public async Task<int> GetUserCreditsAsync(int userId)
    {
        return await _userRepository.GetCreditsAsync(userId);
    }
}

