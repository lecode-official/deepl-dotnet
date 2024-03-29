# Migration Guide

This unofficial .NET binding for DeepL will be discontinued in favor of the [official .NET binding for DeepL](https://github.com/DeepLcom/deepl-dotnet), soon. This migration guide, will help you to make the transition as easy as possible.

## Updating Package References

Before migrating to the official .NET binding for DeepL, you have to uninstall the old binding first. If you use Visual Studio, just open the NuGet package manager, select the `DeepL` package and uninstall it. If you use the package manager console, the .NET CLI, or the Paket package manager, you can use one of the following commands:

```bash
Uninstall-Package DeepL # Package Manager
dotnet remove package DeepL # .NET CLI
paket remove DeepL # Paket CLI
```

Alternatively, you can also manually remove the reference from your project file, by locating and removing the following line:

```xml
<PackageReference Include="DeepL" Version="0.4.3" />
```

Now, that you have uninstalled the old `DeepL` package, you can install the new `DeepL.net` package. Again, you can either use the NuGet package manager UI in Visual Studio or your favorite CLI-based package manager:

```bash
Install-Package DeepL.net -Version 1.5.0 # Package Manager
dotnet add package DeepL.net --version 1.5.0 # .NET CLI
paket add DeepL.net --version 1.5.0 # Paket CLI
```

Alternatively, you can also manually add a reference to your project file:

```xml
<PackageReference Include="DeepL.net" Version="1.5.0" />
```

## Initialization

After you have updated your package references to use the official DeepL .NET binding, you can start updating your code. The central class of the old `DeepL` package was the `DeepLClient`, whereas the new package uses the `Translator` class. Both have the authentication key as their first argument. Unlike the old `DeepLClient` class, the new `Translator` class does not require you to specify whether you want to use the paid or the free tier of DeepL, because `Translator` automatically detects whether your authentication key is for the paid or the free tier. So, you have update your initialization code from this:

```csharp
using (DeepLClient client = new DeepLClient("<authentication key>", useFreeApi: false))
{
}
```

to this:

```csharp
using (Translator translator = new Translator("<authentication key>))
{
}
```

Optionally, you can now specify a configuration object of type `TranslatorOptions` as a second argument, which can be used to customize the number of network retries, the amount of time before timing out, and to configure a proxy.

## Translating Text

The old `DeepLClient` class had the `TranslateAsync` family of methods for translating text. These are now superseded by the `TranslateTextAsync` family of methods in the new `Translator` class. Specifying the source and target language is a lot easier with the new DeepL .NET binding. The old DeepL .NET binding allowed you to either specify a language directly as a language code, via the `Language` enumeration, or via a `SupportedLanguage` object, which could be retrieved from the `GetSupportedLanguagesAsync` method. The new DeepL .NET binding only supports the direct specification of a language code as a string, but to make things easier, they have a static `LanguageCode` class, which contains constants for all supported languages. This means, that instead of the myriad of overloads of `TranslateAsync` that were contained in `DeepLClient`, the new `Translator` class only has two overloads for `TranslateTextAsync`: one for translating a single string and one for translating multiple strings. The following table contains some examples of how to update your calls to `TranslateAsync`.

| **Old**                                                                                                                        | **New**                                                                                                                              |
|--------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------|
| `await client.TranslateAsync("This is a test.", Language.English, Language.German);`                                           | `await translator.TranslateTextAsync("This is a test.", LanguageCode.English, LanguageCode.German);`                                 |
| `await client.TranslateAsync("This is a test.", Language.German);`                                                             | `await translator.TranslateTextAsync("This is a test.", null, LanguageCode.German);`                                                 |
| `await client.TranslateAsync("This is a test.", "en", "de");`                                                                  | `await translator.TranslateTextAsync("This is a test.", "en", "de");`                                                                |
| `await client.TranslateAsync("This is a test.", "de");`                                                                        | `await translator.TranslateTextAsync("This is a test.", null, "de");`                                                                |
| `await client.TranslateAsync(new List<string> { "This is a test.", "And another test." }, Language.English, Language.German);` | `await translator.TranslateTextAsync(new [] { "This is a test.", "And another test." }, LanguageCode.English, LanguageCode.German);` |
| `await client.TranslateAsync(new List<string> { "This is a test.", "And another test." }, Language.German);`                   | `await translator.TranslateTextAsync(new [] { "This is a test.", "And another test." }, null, LanguageCode.German);`                 |
| `await client.TranslateAsync(new List<string> { "This is a test.", "And another test." }, "en", "de");`                        | `await translator.TranslateTextAsync(new [] { "This is a test.", "And another test." }, "en", "de");`                                |
| `await client.TranslateAsync(new List<string> { "This is a test.", "And another test." }, "de");`                              | `await translator.TranslateTextAsync(new [] { "This is a test.", "And another test." }, null, "de");`                                |

The old `TranslateAsync` methods for translating a single string returned an instance of the `Translation` class, which contained the detected source language and the resulting translation. The equivalent result class of the new `TranslateTextAsync` method for translating a single string is `TextResult`. They both contain the same two properties: `DetectedSourceLanguage` and `Text`, so the only thing that you have to do is to update the name of the result class:

```csharp
 // Old way
Translation translation = await client.TranslateAsync("This is a test.", Language.German);
Console.WriteLine(translation.DetectedSourceLanguage);
Console.WriteLine(translation.Text);

// New way
TextResult textResult = await translator.TranslateTextAsync("This is a test.", null, LanguageCode.German);
Console.WriteLine(textResult.DetectedSourceLanguage);
Console.WriteLine(textResult.Text);
```

For ease of use, the new `TextResult` class implements the `ToString` method, which means, that it can be directly cast to a string:

```csharp
TextResult textResult = await translator.TranslateTextAsync("This is a test.", null, LanguageCode.German);
Console.WriteLine(textResult);
```

Also, all the translation options can now be specified via the `TextTranslateOptions` class, that can optionally be passed to `TranslateTextAsync` as argument. The `TextTranslateOptions` can be used to specify the formality of the translation, whether to preserve formatting, how to split sentences, how to handle XML, and the glossary that is to be used. In the old `DeepLClient`, these could directly be specified in the `TranslateAsync` methods using the parameters `splitting`, `preserveFormatting`, `formality`, and `xmlHandling`. 

## Translating Documents

To translate documents like Word files and PDFs, the old `DeepLClient` offered the `TranslateDocumentAsync` family of methods, which allowed you to upload a file to the DeepL API, translate it, and download the translated document. `DeepLClient` offered a whole host of overloads, which accepted streams and file names for the source document and either returned a stream containing the translated document or required an output file name as argument to which the translated document would be downloaded. Also, there were overloads that supported the `Language` enumeration, language code strings, and `SupportedLanguage` objects as the source and target languages. The new DeepL binding for .NET makes this much easier. The `Translator` class has two overloads for the `TranslateDocumentAsync` method: one which receives a `FileInfo` object for the input file and a `FileInfo` object for the output file, and another which receives an input stream, an input file name for the input file, and an output stream for the output file. The following table contains some examples of how to update your calls to `TranslateAsync`.

| **Old**                                                                                                                                                  | **New**                                                                                                                                                |
|----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------|
| `FileStream outputStream = await client.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, Language.English, Language.German);`   | `await translator.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, outputStream, LanguageCode.English, LanguageCode.German);` |
| `FileStream outputStream = await client.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, Language.German);`                     | `await translator.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, outputStream, null, LanguageCode.German);`                 |
| `FileStream outputStream = await client.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, "en", "de");`                          | `await translator.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, outputStream, "en", "de");`                                |
| `FileStream outputStream = await client.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, "de");`                                | `await translator.TranslateDocumentAsync(new FileStream(fileName, FileMode.Open), fileName, outputStream, null, "de");`                                |
| `FileStream outputStream = await client.TranslateDocumentAsync(inputFileName, outputFileName, Language.English, Language.German);`                       | `await translator.TranslateDocumentAsync(new FileInfo(inputFileName), new FileInfo(outputFileName), LanguageCode.English, LanguageCode.German);`       |
| `FileStream outputStream = await client.TranslateDocumentAsync(inputFileName, outputFileName, Language.German);`                                         | `await translator.TranslateDocumentAsync(new FileInfo(inputFileName), new FileInfo(outputFileName), null, LanguageCode.German);`                       |
| `FileStream outputStream = await client.TranslateDocumentAsync(inputFileName, outputFileName, "en", "de");`                                              | `await translator.TranslateDocumentAsync(new FileInfo(inputFileName), new FileInfo(outputFileName), "en", "de");`                                      |
| `FileStream outputStream = await client.TranslateDocumentAsync(inputFileName, outputFileName, "de");`                                                    | `await translator.TranslateDocumentAsync(new FileInfo(inputFileName), new FileInfo(outputFileName), null, "de");`                                      |

The translation options can now be specified via the `DocumentTranslateOptions` class, that can optionally be passed to `TranslateDocumentAsync` as argument. The `DocumentTranslateOptions` can be used to specify the formality of the translation and the glossary that is to be used. In the old `DeepLClient`, the formality could directly be specified in the `TranslateDocumentAsync` methods using the `formality` parameter.

Just as the old `TranslateDocumentAsync` method handled the upload, the wait until the translation is complete, and the download of the translated document for you, so does the the new `TranslateDocumentAsync` method. If, however, your application requires you to perform these steps manually, the old `DeepLClient` offered you the `UploadDocumentForTranslationAsync`, `CheckTranslationStatusAsync`, and `DownloadTranslatedDocumentAsync` methods. `UploadDocumentForTranslationAsync` uploaded the document file to the DeepL API and requested its translation. `CheckTranslationStatusAsync`, checked the status of the translation, i.e., whether the translation has finished yet and if not how many seconds are left. After the translation has finished, the `DownloadTranslatedDocumentAsync` method, downloaded the translated document from the DeepL API. The new .NET binding for DeepL also provides this functionality using the methods `TranslateDocumentUploadAsync`, for uploaded the document and requesting its translation, `TranslateDocumentStatusAsync` for checking the translation status, and `TranslateDocumentDownloadAsync` for downloading the translated document. Furthermore, they also offer another convenience method, `TranslateDocumentWaitUntilDoneAsync`, which continuously checks the translation status of your document asynchronously and returns once the translation has finished (or an error occurred). The `TranslateDocumentUploadAsync` works just like the `TranslateDocumentAsync` method (without having to specify an output stream or an output `FileInfo` object), but it returns right after uploading and requesting the translation. It returns a `DocumentHandle` object, which contains the ID of the document (via the `DocumentId` property) and the key associated with the document translation (via the `DocumentKey`). The document key is a unique key that is used to encrypt the uploaded document as well as the resulting translation on the server side. The `DocumentHandle` class is equivalent to the old `DocumentTranslation` class of the old DeepL .NET binding and the property names are exactly the same. So, in order to migrate you only have to change the name of the type. The `TranslateDocumentStatusAsync` requires you to pass the `DocumentHandle` object received from the `TranslateDocumentUploadAsync` and returns a `DocumentStatus` object, which contains the document ID (via the `DocumentId` property), the status (via the `Status` property), the number of seconds remaining until the translation is complete (via the `SecondsRemaining` property), the number of characters that were billed for the translation of the document (via the `BilledCharacters` property), an error message if an error occurred (via the `ErrorMessage` property), and two convenience properties: `Ok` and `Done`, which evaluate to `true` if no error occurred and if the translation has finished, respectively. The status is of type `StatusCode`, which is an enumeration that has the values `Queued`, `Translating`, `Done`, and `Error`. These are exactly the same names as in the old `TranslationState` enumeration, which was returned by the `CheckTranslationStatusAsync` method, so you only have to change the enumeration type name from `TranslationState` to `StatusCode`. The `TranslateDocumentWaitUntilDoneAsync` works just like the `TranslateDocumentStatusAsync` method, but instead of returning the current translation status it returns once the translation has finished. Finally, the `TranslateDocumentDownloadAsync` method takes the `DocumentHandle` object as argument as well as an output stream or an output `FileInfo` object, and downloads the translated document. The following listing shows the old way and the new way of manually translating documents:

```csharp
// Old way
DocumentTranslation documentTranslation = await this.UploadDocumentForTranslationAsync(inputFileName, Language.German);
while (true)
{
    TranslationStatus translationStatus = await this.CheckTranslationStatusAsync(documentTranslation);
    if (translationStatus.State == TranslationState.Done)
        break;
    if (translationStatus.State == TranslationState.Error)
        throw new InvalidOperationException("An unknown error occurred during the translation of the document.");
    await Task.Delay(translationStatus.SecondsRemaining.HasValue ? translationStatus.SecondsRemaining.Value * 1000 : 1000);
}
await this.DownloadTranslatedDocumentAsync(documentTranslation, outputFileName);

// New way
try
{
    DocumentHandle handle = await TranslateDocumentUploadAsync(new FileInfo(inputFileName), null, LanguageCode.German);
    await TranslateDocumentWaitUntilDoneAsync(handle);
    while (status.Ok && !status.Done)
    {
        await Task.Delay(status.SecondsRemaining.HasValue ? status.SecondsRemaining.Value * 1000 : 1000);
        status = await TranslateDocumentStatusAsync(handle, cancellationToken);
    }
    await TranslateDocumentDownloadAsync(handle, new FileInfo(outputFileName));
}
catch (Exception exception)
{
    throw new InvalidOperationException($"Error occurred during document translation: {exception.Message}", exception);
}

// Alternative new way with easier waiting for the translation to finish
try
{
    DocumentHandle handle = await TranslateDocumentUploadAsync(new FileInfo(inputFileName), null, LanguageCode.German);
    await TranslateDocumentWaitUntilDoneAsync(handle);
    await TranslateDocumentDownloadAsync(handle, new FileInfo(outputFileName));
}
catch (Exception exception)
{
    throw new InvalidOperationException($"Error occurred during document translation: {exception.Message}", exception);
}
```

## Listing Available Languages

The old `DeepLClient` class had a method called `GetSupportedLanguagesAsync`, which returned a list of `SupportedLanguage` objects that contain the currently supported languages of the DeepL translation service. Each `SupportedLanguage` object contains the language code (via the `LanguageCode` property) and the name of the language (vie the `Name` property). The `SupportedLanguage` objects could directly be used in the `TranslateAsync`, `UploadDocumentForTranslationAsync`, and `TranslateDocumentAsync` methods to specify the source and target language. The problem here was, that some of the language codes retrieved could only be used as target languages and some only as source languages. The new DeepL .NET binding solves this by having two separate methods: `GetSourceLanguagesAsync`, which returns an array of `SourceLanguage` objects, and `GetTargetLanguagesAsync`, which returns an array of `TargetLanguage` objects. They both inherit from `Language`, which contains two public properties: `Name` and `Code`. `Language`, in contrast to the old `SupportedLanguage`, has a lot of convenience methods built-in. For example, it can be implicitly cast to a string, which means, that it can also be directly used in all translation methods, because the compiler will automatically cast the `Language` object to a string containing the language code. `Language` also implements `IEquatable` and has a property for creating a `CultureInfo` from the language code.

## Monitoring Usage

In order to monitor the usage of your DeepL API subscription, the old `DeepLClient` offered the `GetUsageStatisticsAsync` method. This method returns a `UsageStatistics` object, which contains the number of characters that you have already translated using the DeepL API as well as the character limit. The new `Translator` also allows you to monitor your usage, but it offers much more fine-grained information. In order to retrieve your usage statistics, you can use the `GetUsageAsync` method, which returns a `Usage` object. This object does not only provide you with the number of characters translated and the character limit, but it splits your usage up into character usage (via the `Character` property), document usage (via the `Document` property), and team document usage (via the `TeamDocument` property). Each of them expose a `Count` property, which contains the currently used number of items for the usage type, and a `Limit` property, which contains the maximum permitted number of items for the usage type. For your convenience, they also expose a `LimitReached` property, which is `true` if the limit was reached and `false` otherwise. Furthermore, the `Usage` object has a `AnyLimitReached`, which is `true` if the limit of any usage type has been reached and `false` otherwise.

## Handling Exceptions

The old DeepL binding for .NET only had a single exception class, which was used for all errors that could occur: `DeepLException`. The only way to differentiate between different errors was to check the `Message` property of the exception, which contained some details. The new DeepL .NET binding contains many exception types, one for each error that can occur during its usage. Fortunately, they all derive from a single base class, which is called `DeepLException`. This makes it extremely simple to migrate to the new exception type: if you are not interested in the exact error that occurred, you can just keep the same exception. But, if you are interested in the exact error that occurred, the new .NET binding offers the following exception types. For more information, please refer to the official documentation.

- `DeepLException`
  - `AuthorizationException`
  - `NotFoundException`
    - `GlossaryNotFoundException`
  - `QuotaExceededException`
  - `TooManyRequestsException`
  - `ConnectionException`
  - `DocumentNotReadyException`
  - `DocumentTranslationException`

## DeepL CLI

If you were using the command line interface for DeepL, then unfortunately, there is currently no official alternative. If you want a command line interface for DeepL, I suggest you [open an issue](https://github.com/DeepLcom/deepl-dotnet/issues) with the official project.

## New Features

A new feature, which was missing from the old DeepL .NET binding are glossaries. By migrating to the official .NET binding for DeepL, you will be able to use glossaries, which enable you to customize the translation of certain terms. This can, for example, be helpful, when you are translating texts from a specific domain that contains technical terms, that might be translated differently in other circumstances. New glossaries can be created using the `CreateGlossaryAsync` of the `Translator` class. To retrieve existing glossaries, you can use the `ListGlossariesAsync`, which returns a list of all the glossaries that were created so far. Finally, to use a glossary, you have to specify its ID in the `TextTranslateOptions` or the `DocumentTranslateOptions` either by passing the `GlossaryInfo` object, retrieved from the `ListGlossariesAsync` method, to the constructor, or by setting the `GlossaryId` property. For a more complete introduction, please refer to the [official documentation](https://github.com/DeepLcom/deepl-dotnet#glossaries).
