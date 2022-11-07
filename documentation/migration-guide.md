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

Now, that you have uninstalled the old `DeepL` package, you can install the new `DeepL.net` package. Again, you can either use the NuGet package manager UI in Visual Studio or your favorite CLI-based package managers:

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

After you have updated your package references to use the official DeepL .NET binding, you have to start updating you code. The central class of the old `DeepL` package was `DeepLClient`, whereas the new package uses the `Translator` class. Both have the authentication key as their first argument. Unlike the old `DeepLClient` class, the new `Translator` class does not require you to specify whether you want to use the paid or the free tier of DeepL, because `Translator` automatically detects whether your authentication key is for the paid or the free tier. So, you have update your initialization code from this:

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

Optionally, you can now specify a configuration object of type `TranslatorOptions` as a second argument, which can be used to customize the number of network retries, the amount of time before timing out, and a proxy.

## Translating Text

The old `DeepLClient` class had the `TranslateAsync` family of methods for translating text. These are now superseded by the `TranslateTextAsync` family of methods in the new `Translator` class. Specifying the source and target language is a lot easier with the DeepL .NET binding. The old DeepL .NET binding allowed you to either specify a language directly as a language code, via the `Language` enumeration, or via a `SupportedLanguage` object, which could be retrieved from the `GetSupportedLanguagesAsync` method. The new DeepL .NET binding only supports the direct specification of a language code as a string, but to make things easier, they have a static `LanguageCode` class, which contains constants for all supported languages. This means, that instead of the myriad of overloads of `TranslateAsync` that were contained in `DeepLClient`, the new `Translator` class only has two overloads for `TranslateTextAsync`: one for translating a single string and one for translating multiple strings. The following table contains some examples of how to update your calls to `TranslateAsync`.

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

The old `TranslateAsync` methods for translating a single string returned an instance of the `Translation` class, which contained the detected source language and the resulting translation. The equivalent result class of the new `TranslateTextAsync` methods for translating a single string is `TextResult`. They both contain the same two properties: `DetectedSourceLanguage` and `Text`, so the only thing that you have to do is to update the name of the result class:

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

Also, all the translation options can now be specified via the `TextTranslateOptions` class, that can optionally be passed to `TranslateTextAsync` as argument. The `TextTranslateOptions` can be used to specify the formality of the translation, whether to preserve formatting, how to split sentences, and XML handling. In the old `DeepLClient`, these could directly be specified in the `TranslateAsync` methods using the parameters `splitting`, `preserveFormatting`, `formality`, and `xmlHandling`. 

## Translating Documents



## Listing Available Languages



## Monitoring Usage



## New Features


