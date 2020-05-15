# Other Functions

## Monitoring Usage

In order to monitor your usage of the DeepL API, you can use the `GetUsageStatisticsAsync` method of the `DeepLClient`. This method returns a `UsageStatistics` object, which contains the number of characters than you have already translated using the DeepL API as well as the character limit.

```csharp
using (DeepLClient client = new DeepLClient("<authentication key>")
{
    UsageStatistics usageStatistics = await client.GetUsageStatisticsAsync();
    Console.WriteLine(usageStatistics.CharacterCount);
    Console.WriteLine(usageStatistics.CharacterLimit);
}
```

## Listing Supported Languages

The `GetSupportedLanguagesAsync` method of the `DeepLClient` lists all languages that are supported by the DeepL API. The method returns `IEnumerable<SupportedLanguage>`. Each `SupportedLanguage` object contains the language code (`LanguageCode`) and the name of the language (`Name`). `SupportedLanguage` objects can be used as arguments for all translation-related methods in the `DeepLClient` instead of the raw language code or the `Language` enumeration.

```csharp
using (DeepLClient client = new DeepLClient("<authentication key>")
{
    IEnumerable<SupportedLanguage> languages = await client.GetSupportedLanguagesAsync();
    foreach (SupportedLanguage language in languages)
    {
        Console.WriteLine(language.Name);
        Console.WriteLine(language.LanguageCode);
    }

    Translation translation = await client.TranslateAsync(
        "This is a test sentence.",
        languages.First()
    );
    Console.WriteLine(translation.DetectedSourceLanguage);
    Console.WriteLine(translation.Text);
}
```
