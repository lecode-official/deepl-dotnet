# DeepL.NET Documentation

Welcome to the documentation of DeepL.NET. DeepL.NET is a C# library for accessing the DeepL translation REST API. Please note that DeepL.NET only support v2 of the API, as v1 is now a deprecated. In oder to use DeepL.NET you need to have a [DeepL API subscription](https://www.deepl.com/pro.html#developer). For further information about the DeepL API, please refer to the [official documentation](https://www.deepl.com/docs-api/introduction/).

## Installation

To get started you have to add the package reference to your project. Use one of the following commands depending on your development environment (Package Manager, if you use Visual Studio, .NET CLI if you use .NET on the command line or Visual Studio Code, or Paket CLI, if you use the Paket package manager):

```bash
Install-Package DeepL -Version 0.1.0 # Package Manager
dotnet add package DeepL --version 0.1.0 # .NET CLI
paket add DeepL --version 0.1.0 # Paket CLI
```

Alternatively, you can also manually add a reference to your project file:

```xml
<PackageReference Include="DeepL" Version="0.1.0" />
```

## Getting Started

After installing the NuGet package, you can start using DeepL.NET. All functionality of DeepL.NET resides in the namespace `DeepL`, so make sure to import the namespace (`using DeepL`). The central class in the library is the `DeepLClient`, which you have to instantiate to access DeepL API. `DeepLClient` implements `IDisposable`, so it is recommended that you either encapsulate its usage in a `using` block or implement `IDisposable` yourself. The `DeepLClient` has no internal state so a single instance can be used throughout you whole application. The only argument of the `DeepLClient` constructor is the authentication key for the DeepL API, which you will find in the [account settings](https://www.deepl.com/pro-account.html) after creating a DeepL API subscription.

```csharp
using (DeepLClient client = new DeepLClient("<authentication key>")
{
}
```

The easiest way to translate text is via the `TranslateAsync` method, which takes a text and a target language as arguments and returns a `Translation` object. The `Translation` object contains the translated text as well as the source language that was automatically inferred from the text.

```csharp
Translation translation = await client.TranslateAsync("This is a test sentence.", Language.German);
Console.WriteLine(translation.DetectedSourceLanguage);
Console.WriteLine(translation.Text);
```

## Further Topics

For more advanced topics you may refer to the following topics.

- [Translating Text](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/translating-text.md)
- [Translating Documents]((https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/translating-documents.md))
- [Other Functions]((https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/other-functions.md))
