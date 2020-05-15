# Translating Text

## Translating Simple Text

The `DeepLClient.TranslateAsync(string, ...)` family of methods can be used to translate texts. At the bare minimum, the text that is to be translated and the target language have to be specified. Below you find a table with all possible arguments. All methods return a `Translation` object, which contains the translated text (`Text`) as well as the source language (`DetectedSourceLanguage`). The source language either reflects the source language specified as an argument or it is the language that the translation engine inferred from the source text.

| Parameter | Type | Optional | Description |
|-----------|------|:--------:|-------------|
| `text` | `string` | ✗ | The text that is to be translated. |
| `sourceLanguageCode` or `sourceLanguage` | `string`, `Language`, or `SupportedLanguage` | ✓ | The language of the text that is to be translated. If not specified, the language is inferred from the text, if possible. |
| `targetLanguageCode` or `targetLanguage` | `string`, `Language`, or `SupportedLanguage` | ✗ | The language into which the text is to be translated. |
| `splitting` | `Splitting` | ✓ | Determines how the text is split into sentences by the translation engine. Defaults to `Splitting.InterpunctionAndNewLines`. |
| `preserveFormatting` | `bool` | ✓ | Sets whether the translation engine should respect the original formatting, even if it would usually correct some aspects. This includes capitalization at the beginning of sentences and interpunction. Defaults to `false`. |
| `xmlHandling` | `XmlHandling` | ✓ | Determines how XML tags are handled by the translation engine. Defaults to `null`. |
| `cancellationToken` | `CancellationToken` | ✓ | Can be used to cancel a long running translation. Defaults to `null`. |

```csharp
Translation translation = await client.TranslateAsync(
    "This is a test sentence.",
    Language.English,
    Language.German,
    Splitting.InterpunctionAndNewLines,
    preserveFormatting: false
);
Console.WriteLine(translation.DetectedSourceLanguage);
Console.WriteLine(translation.Text);
```

## Translating Multiple Texts

The `text` argument may contain multiple sentences. Unfortunately, the DeepL translation engine can only handle texts up to a certain size and will cut off overly long sentences. Normally, the splitting of the translation engine (which can be configured using the `splitting` parameter), will take care of this, but in some cases, this may fail. The `DeepLClient.TranslateAsync(IEnumerable<string>, ...)` family of methods allows you to send multiple texts to the DeepL API at once. They share all the same parameters as the `DeepLClient.TranslateAsync(string, ...)` methods, but the texts have to specified as a list. The batch translation methods return `IEnumerable<Translation>`, one for each text. Up to 50 texts can be send to the API in one call.

```csharp
IEnumerable<Translation> translations = await client.TranslateAsync(
    new List<string>
    {
        "This is a test sentence.",
        "This is another test sentence."
    },
    Language.German
);
foreach (Translation translation in translations)
{
    Console.WriteLine(translation.DetectedSourceLanguage);
    Console.WriteLine(translation.Text);
}
```

### Translating Large Volumes of Text

When you are trying to translate a large volume of text, then you should consider splitting the translations into multiple calls to the API, as each request has a limited size. For example you could split your text into paragraphs and send one request per paragraph. Multiple calls can also be made from different threads/processes.

## Languages

As of writing this documentation (May, 2020), the DeepL API supports the following languages:

| Language | Code | `Language` Enumeration |
|----------|------|------------------------|
| German | DE | `Language.German` |
| English | EN | `Language.English` |
| Spanish | ES | `Language.Spanish` |
| French | FR | `Language.French` |
| Italian | IT | `Language.Italian` |
| Dutch | NL | `Language.Dutch` |
| Polish | PL | `Language.Polish` |
| Portuguese | PT | `Language.Portuguese` |
| Portuguese (Brazilian) | PT-BR | `Language.BrazilianPortuguese` |
| Russian | RU | `Language.Russian` |
| Japanese | JA | `Language.Japanese` |
| Chinese | ZH | `Language.Chinese` |

> **IMPORTANT**: Please note that Brazilian Portuguese (PT-BR) is only supported as a target language and not as a source language. Portuguese (PT) includes all varieties of Portuguese including Brazilian Portuguese.

Every translation method has an overload that accepts the language code as a string. For your convenience, there is also the `Language` enumeration, which contains all languages (supported as of May, 2020). Furthermore, the source or target language can also be specified as an instance of `SupportedLanguage`. A list of all supported languages can be retrieved using the `GetSupportedLanguagesAsync` method. For further information see ["Listing Supported Languages"](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/other-functions.md#listing-supported-languages). This is helpful for inclusion in user interfaces or in case a new supported language is added, but DeepL.NET has not been updated to include it.

## Text Splitting

The translation engine has a maximum number of characters it can translate at a time, so, by default, it tries to split your text into smaller chunks by breaking it at interpunction (sentence delimiters like periods, question marks, or exclamation marks) or at line breaks. Sometimes the splitting does not yield satisfactory results, so this behavior can be controlled using the `splitting` parameter available in all translation methods of `DeepLClient`. It supports values of `Splitting.None`, meaning the text is never split automatically, `Splitting.Interpunction`, meaning the text is split at interpunction symbols, and `Splitting.InterpunctionAndNewLines`, meaning the text is split at interpunction symbols and line breaks. Another way of handling splitting is by splitting the text manually and sending the chunks separately to the DeepL API.

## Handling XML

> **IMPORTANT**: As of writing this documentation (May, 2020), the API does not yet support XML handling for Japanese or Chinese texts.

There are two scenarios where it is helpful to handle XML input when translating texts. The first scenario is obviously when the text in question already contains XML markup. In that case the use of `XmlHandling` can help the translation engine to correctly handle the tags used in text. The second scenario is not as obvious: sometimes texts contains characters or character sequences that may have a special meaning to the application. When translating these kind of texts, the special characters or character sequences may either be removed from the translation or may be translated as well. In order to prevent this from happening, the application can convert these characters or character sequences to XML tags and use `XmlHandling` to prevent the translation engine from doing anything with these characters or character sequences. XML tags are always anchored to the word directly preceding it and will be added to corresponding translated word in the translation. So the actual positioning of the XML tags in the translation may change.
