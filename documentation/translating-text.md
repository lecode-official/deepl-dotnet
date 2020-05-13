# Translating Text

## Translating Simple Text

The `DeepLClient.TranslateAsync(string, ...)` family of methods can be used to translate texts. At the bare minimum, the text that is to be translated and the target language have to be specified. Below you find a table with all possible arguments. All methods return a `Translation` object, which contains the translated text (`Text`) as well as the source language (`DetectedSourceLanguage`). The source language either reflects the source language specified as an argument or it is the language that the translation engine inferred from the source text.

| Parameter | Type | Optional | Description |
|-----------|------|:--------:|-------------|
| `text` | `string` | ✖ | The text that is to be translated. |
| `sourceLanguageCode` or `sourceLanguage` | `string`, `Language`, or `SupportedLanguage` | ✔ | The language of the text that is to be translated. This can either be specified as a language code (e.g. "EN" or "DE"), as a value of the `Language` enumeration, or as a `SupportedLanguage` that can be retrieved from the `GetSupportedLanguagesAsync` method. If not specified, the language is inferred from the text, if possible. |
| `targetLanguageCode` or `targetLanguage` | `string`, `Language`, or `SupportedLanguage` | ✖ | The language into which the text is to be translated. This can either be specified as a language code (e.g. "EN" or "DE"), as a value of the `Language` enumeration, or as a `SupportedLanguage` that can be retrieved from the `GetSupportedLanguagesAsync` method. |
| `splitting` | `Splitting` | ✔ | Determines how the text is split into sentences on the server-side. Supported Values:  `None` - The text is never split `Interpunction` - The text is split at interpunction symbols like a period `InterpunctionAndNewLines` - The text is split at interpunction symbols and new lines  Defaults to InterpunctionAndNewLines. |
| `preserveFormatting` | `bool` | ✔ | Sets whether the translation engine should respect the original formatting, even if it would usually correct some aspects. This includes capitalization at the beginning of sentences and interpunction. Defaults to `false`. |
| `xmlHandling` | `XmlHandling` | ✔ | Determines how XML tags are handled by the translation engine. For more information, please refer to the section "Handling of XML". Defaults to `null`. |
| `cancellationToken` | `CancellationToken` | ✔ | Can be used to cancel a long running translation. Defaults to `null`. |

## Translating Multiple Texts

The `text` argument may contain multiple sentences. Unfortunately, the DeepL translation engine can only handle texts up to a certain size and will cut off overly long sentences. Normally, the splitting of the translation engine (which can be configured using the `splitting` parameter), will take care of this, but in some cases, this may fail. The `DeepLClient.TranslateAsync(IEnumerable<string>, ...)` family of methods allows you to send multiple texts to the DeepL API at once. They share all the same parameters as the `DeepLClient.TranslateAsync(string, ...)` methods, but the texts have to specified as a list. The batch translation methods return `IEnumerable<Translation>`, one for each text. Up to 50 texts can be send to the API in one call.

## Translating Large Volumes of Text

When you are trying to translate a large volume of text, then you should consider splitting the translations into multiple calls to the API, as each request has a limited size. For example you could split your text into paragraphs and send one request per paragraph. Multiple calls can also be made from different threads/processes.

## Handling of XML

(*Coming soon*)
