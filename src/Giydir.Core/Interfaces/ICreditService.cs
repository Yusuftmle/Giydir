namespace Giydir.Core.Interfaces;

public interface ICreditService
{
    Task<CreditCheckResult> CheckCreditsAsync(int userId, int required);
    Task<int> GetUserCreditsAsync(int userId);
}

public class CreditCheckResult
{
    public bool HasEnough { get; set; }
    public int CurrentCredits { get; set; }
    public int Required { get; set; }
}



