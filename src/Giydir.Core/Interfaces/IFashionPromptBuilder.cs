using Giydir.Core.DTOs;

namespace Giydir.Core.Interfaces;

public interface IFashionPromptBuilder
{
    Task<string> BuildPromptAsync(FashionRenderRequest request);
    string BuildNegativePrompt(FashionRenderRequest request);
}
