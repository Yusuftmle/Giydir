using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using System.Text.Json;

namespace Giydir.Infrastructure.Services;

public class PromptService : IPromptService
{
    public string GeneratePromptFromTemplate(Template template, string? customPrompt = null)
    {
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }

        // Template'den prompt oluştur
        if (!string.IsNullOrEmpty(template.PromptTemplate))
        {
            return template.PromptTemplate;
        }

        // Eğer PromptTemplate yoksa JSON'dan oluştur
        return GeneratePromptFromJson(
            template.Style,
            template.Color,
            template.Pattern,
            template.Material,
            template.Category,
            template.AdditionalAttributes
        );
    }

    public string GeneratePromptFromJson(string style, string color, string pattern, string material, string category, string? additionalAttributes = null)
    {
        var promptParts = new List<string>();

        // Kategori bazlı başlangıç
        switch (category.ToLower())
        {
            case "upper_body":
                promptParts.Add("professional");
                break;
            case "lower_body":
                promptParts.Add("fashion");
                break;
            case "dresses":
                promptParts.Add("elegant");
                break;
        }

        // Renk
        if (!string.IsNullOrEmpty(color))
        {
            promptParts.Add(color);
        }

        // Materyal
        if (!string.IsNullOrEmpty(material))
        {
            promptParts.Add(material);
        }

        // Stil
        if (!string.IsNullOrEmpty(style))
        {
            promptParts.Add(style);
        }

        // Desen
        if (!string.IsNullOrEmpty(pattern) && pattern.ToLower() != "solid")
        {
            promptParts.Add($"{pattern} pattern");
        }

        // Kategoriye göre ürün tipi
        switch (category.ToLower())
        {
            case "upper_body":
                promptParts.Add("shirt or top");
                break;
            case "lower_body":
                promptParts.Add("pants or trousers");
                break;
            case "dresses":
                promptParts.Add("dress");
                break;
        }

        // Ekstra özellikler
        if (!string.IsNullOrEmpty(additionalAttributes))
        {
            try
            {
                var attrs = JsonSerializer.Deserialize<Dictionary<string, string>>(additionalAttributes);
                if (attrs != null)
                {
                    foreach (var attr in attrs)
                    {
                        promptParts.Add(attr.Value);
                    }
                }
            }
            catch
            {
                // JSON parse hatası - ignore
            }
        }

        // Standart eklemeler
        promptParts.Add("clean white background");
        promptParts.Add("studio lighting");
        promptParts.Add("high quality fashion photography");
        promptParts.Add("professional product image");

        return string.Join(", ", promptParts);
    }
}

