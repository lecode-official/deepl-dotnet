# Translating Text

## Translating Simple Text

The `DeepLClient.TranslateAsync(string, ...)` family of methods can be used to translate texts. At a bare minimum, the text that is to be translated and the target language have to be specified. Below you find a table with all possible arguments. All methods return a `Translation` object, which contains the translated text (`Text`) as well as the source language (`DetectedSourceLanguage`). The source language either reflects the source language specified as an argument or it is the language that the translation engine inferred from the source text.

| Parameter                                | Type                                         | Optional | Description                                                                                                                                                                                                                         |
|------------------------------------------|----------------------------------------------|:--------:|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `text`                                   | `string`                                     | ✗        | The text that is to be translated.                                                                                                                                                                                                  |
| `sourceLanguageCode` or `sourceLanguage` | `string`, `Language`, or `SupportedLanguage` | ✓        | The language of the text that is to be translated. If not specified, the language is inferred from the text, if possible.                                                                                                           |
| `targetLanguageCode` or `targetLanguage` | `string`, `Language`, or `SupportedLanguage` | ✗        | The language into which the text is to be translated.                                                                                                                                                                               |
| `splitting`                              | `Splitting`                                  | ✓        | Determines how the text is split into sentences by the translation engine. Defaults to `Splitting.InterpunctionAndNewLines`.                                                                                                        |
| `preserveFormatting`                     | `bool`                                       | ✓        | Determines whether the translation engine should respect the original formatting, even if it would usually correct some aspects. This includes capitalization at the beginning of sentences and interpunction. Defaults to `false`. |
| `xmlHandling`                            | `XmlHandling`                                | ✓        | Determines how XML tags are handled by the translation engine. Defaults to `null`.                                                                                                                                                  |
| `cancellationToken`                      | `CancellationToken`                          | ✓        | Can be used to cancel a long running translation. Defaults to `default(CancellationToken)`.                                                                                                                                         |

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

| Language                  | Code  | `Language` Enumeration         | Source Language | Target Language |
|---------------------------|-------|--------------------------------|-----------------|-----------------|
| German                    | DE    | `Language.German`              | ✓               | ✓               |
| English                   | EN    | `Language.English`             | ✓               | ✗               |
| British English           | EN-GB | `Language.BritishEnglish`      | ✗               | ✓               |
| American English          | EN-US | `Language.AmericanEnglish`     | ✗               | ✓               |
| French                    | FR    | `Language.French`              | ✓               | ✓               |
| Italian                   | IT    | `Language.Italian`             | ✓               | ✓               |
| Japanese                  | JA    | `Language.Japanese`            | ✓               | ✓               |
| Spanish                   | ES    | `Language.Spanish`             | ✓               | ✓               |
| Dutch                     | NL    | `Language.Dutch`               | ✓               | ✓               |
| Polish                    | PL    | `Language.Polish`              | ✓               | ✓               |
| Portuguese (all variants) | PT    | `Language.Portuguese`          | ✓               | ✗               |
| Portuguese (no Brazilian) | PT-PT | `Language.Portuguese`          | ✗               | ✓               |
| Portuguese (Brazilian)    | PT-BR | `Language.BrazilianPortuguese` | ✗               | ✓               |
| Russian                   | RU    | `Language.Russian`             | ✓               | ✓               |
| Chinese                   | ZH    | `Language.Chinese`             | ✓               | ✓               |

Every translation method has an overload that accepts the language code as a string. For your convenience, there is also the `Language` enumeration, which contains all languages (supported as of May, 2020). Furthermore, the source or target language can also be specified as an instance of `SupportedLanguage`. A list of all supported languages can be retrieved using the `GetSupportedLanguagesAsync` method. For further information see ["Listing Supported Languages"](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/other-functions.md#listing-supported-languages). This is helpful for inclusion in user interfaces or in case a new supported language is added, but DeepL.NET has not been updated to include it.

## Text Splitting

The translation engine has a maximum number of characters it can translate at a time, so, by default, it tries to split your text into smaller chunks by breaking it at interpunction (sentence delimiters like periods, question marks, or exclamation marks) or at line breaks. Sometimes the splitting does not yield satisfactory results, so this behavior can be controlled using the `splitting` parameter available in all translation methods of `DeepLClient`. It supports values of `Splitting.None`, meaning the text is never split automatically, `Splitting.Interpunction`, meaning the text is split at interpunction symbols, and `Splitting.InterpunctionAndNewLines`, meaning the text is split at interpunction symbols and line breaks. Another way of handling splitting is by splitting the text manually and sending the chunks separately to the DeepL API.

## Handling XML

There are two scenarios where it is helpful to handle XML input when translating texts. The first scenario is obviously when the text in question already contains XML markup. In that case using XML handling can help the translation engine to correctly handle the tags used in text. The second scenario is not as obvious: sometimes texts contains characters or character sequences that may have a special meaning to the application. When translating these kind of texts, the special characters or character sequences may either be removed from the translation or may be translated as well. In order to prevent this from happening, the application can convert these characters or character sequences to XML tags and use XML handling to prevent the translation engine from doing anything with these characters or character sequences.

Every overload of the `TranslateAsync` method has a parameter of type `XmlHandling`, which can be used to specify how XMl is to be handled. By default the XML handling is disabled. XML handling can be activated by adding a default instance of `XmlHandling` as an argument. By default, the XML tags are just ignored and copied into the translation. Please note that XML tags are always anchored to the word directly preceding it and will be added to corresponding translated word in the translation. So the actual positioning of the XML tags in the translation may change, but is semantically correct. The `XmlHandling` class also has some properties, which can be used to tailor the handling of the translation engine of XML to your needs.

| Property           | Description                                                                                                                                                                                                                                                                                |
|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `NonSplittingTags` | A list (`IEnumerable<string>`) of XML tags which never split sentences.                                                                                                                                                                                                                    |
| `SplittingTags`    | A list (`IEnumerable<string>`) of XML tags which always split sentences.                                                                                                                                                                                                                   |
| `IgnoreTags`       | A list (`IEnumerable<string>`) of XML tags that indicate text not to be translated.                                                                                                                                                                                                        |
| `OutlineDetection` | Determines whether the outline of an XML document is automatically detected. Automatic outline detection may not always provide the best translation results, therefore it can make sense to disable it and provide a custom list of splitting and non-splitting tags. Defaults to `true`. |

When translating whole XML documents (in contrast to simple texts, that only contain a few tags), it makes sense to set the splitting argument to `Splitting.Interpunction` to make sure that newlines do not alter the results.

When there are XML tags in you document that group text but do not break sentences, then this tag should be added to `NonSplittingTags`. Otherwise, the translation engine may treat the contents of two adjacent tags as two separate sentences, which may change the meaning of the translation. On the other hand, if there are tags that do split text into separate sentences, then they should be added to the `SplittingTags`. For tags that, for example, contain meta data that should not be translated, then these tags should be added to the list of `IgnoreTags`.

The DeepL translation engine tries to automatically detect the outline of a document. For example `<title>` and `<par>` tags are, by default, `SplittingTags`, while tags like `<i>` are not. This default handling may cause problems and can therefore be disabled by setting `OutlineDetection` to `false`. In that case all splitting and non-splitting tags must be specified manually.

For more detailed information and examples, please refer to the official documentation on [XML handling](https://www.deepl.com/docs-api/handling-xml/).
