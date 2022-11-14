# DeepL.NET Documentation

![DeepL.NET Logo](https://raw.githubusercontent.com/lecode-official/deepl-dotnet/master/documentation/images/banner.png "DeepL.NET Logo")

> :warning: **DEPRECATION NOTICE** For some time now, an [official .NET binding for DeepL](https://github.com/DeepLcom/deepl-dotnet) has been available. This unofficial .NET binding has only been created, because, at the time, there was no official .NET binding. Therefore, this .NET binding will be deprecated soon. This means that this project will only receive security updates and bug fixes, but no new features will be added from now on. Also, the `DeepLClient` class was made obsolete and a compiler warning will be issued, when you use it. On **February 14, 2023**, the project will seize to receive any updates, the repository will be put into archived mode, and the accompanying NuGet package will be deprecated. To help you with the transition, please refer to the [migration guide](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/migration-guide.md)

Welcome to the documentation of DeepL.NET. DeepL.NET is a C# library for accessing the DeepL translation REST API. Please note that DeepL.NET only supports v2 of the API, as v1 is deprecated. In order to use DeepL.NET you need to have a [DeepL API subscription](https://www.deepl.com/pro.html#developer). For further information about the DeepL API, please refer to the [official documentation](https://www.deepl.com/docs-api/introduction/).

## Installation

To get started you have to add the package reference to your project. Use one of the following commands depending on your development environment (Package Manager, if you use Visual Studio, .NET CLI if you use .NET on the command line or Visual Studio Code, or Paket CLI, if you use the Paket package manager):

```bash
Install-Package DeepL -Version 0.4.3 # Package Manager
dotnet add package DeepL --version 0.4.3 # .NET CLI
paket add DeepL --version 0.4.3 # Paket CLI
```

Alternatively, you can also manually add a reference to your project file:

```xml
<PackageReference Include="DeepL" Version="0.4.3" />
```

## Getting Started

After installing the NuGet package, you can start using DeepL.NET. All functionality of DeepL.NET resides in the namespace `DeepL`, so make sure to import the namespace (`using DeepL`). The central class in the library is the `DeepLClient`, which you have to instantiate to access the DeepL API. `DeepLClient` implements `IDisposable`, so it is recommended that you either encapsulate its usage in a `using` block or implement `IDisposable` yourself. The `DeepLClient` has no internal state so a single instance can be used throughout the whole lifetime of your application. The `DeepLClient` constructor has two parameters: the authentication key for the DeepL API, which you will find in the [account settings](https://www.deepl.com/pro-account.html) after creating a DeepL API subscription and whether you want to use the free or the pro version of the DeepL API. The `useFreeApi` parameter defaults to `false`, so it can be omitted.

```csharp
using (DeepLClient client = new DeepLClient("<authentication key>", useFreeApi: false))
{
}
```

The easiest way to translate text is via the `TranslateAsync` method, which takes a text and a target language as arguments and returns a `Translation` object. The `Translation` object contains the translated text as well as the source language that was automatically inferred from the text, if possible.

```csharp
Translation translation = await client.TranslateAsync(
    "This is a test sentence.",
    Language.German
);
Console.WriteLine(translation.DetectedSourceLanguage);
Console.WriteLine(translation.Text);
```

## Error Handling

For any error that may occur during the translation, the `DeepLClient` throws a `DeepLException`. Errors that may happen during translation are as follows:

1. The parameters are invalid (e.g. the source or target language are not supported)
2. The authentication key is invalid
3. The resource could not be found (e.g. when the specified document translation does not exist anymore)
4. The text that is to be translated is too large (although DeepL is known to return the text untranslated in some cases)
5. Too many requests have been made in a short period of time
6. The translation quota has been exceeded
7. An internal server error has occurred
8. The DeepL API server is unavailable

Besides that the `DeepLClient` may also throw other common .NET exceptions, e.g. `ArgumentException` or `ArgumentNullException` for invalid arguments, or I/O related exceptions when uploading or downloading documents. Every method has extensive documentation about each exception that it may throw, but in general it may be enough to just catch `DeepLException`s.

## Asynchronicity

All methods of the `DeepLClient` are asynchronous and non-blocking. The familiar async-await pattern (also known as Task Awaitable Pattern, or TAP) is used throughout the library, so you have to return a `Task` object and mark your methods as `async`.

```csharp
Translation translation = await client.TranslateAsync(
    "This is a test sentence.",
    Language.German
);
```

In case you absolutely have to use DeepL.NET in a synchronous way, store the returned `Task` object, synchronously wait for it to finish, and then retrieve the result (**but this is definitely not recommended**):

```csharp
Task<Translation> task = client.TranslateAsync(
    "This is a test sentence.",
    Language.German
);
task.Wait();
Translation translation = task.Result;
```

All asynchronous methods of the `DeepLClient` offer a `CancellationToken` parameter, which can be used to cancel long-running requests to the DeepL API.

```csharp
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
Translation translation = await client.TranslateAsync(
    "This is a test sentence.",
    Language.German,
    cancellationTokenSource.Token
);
Console.WriteLine("Press enter to cancel the translation...");
Console.ReadLine();
cancellationTokenSource.Cancel();
```

## Further Topics

For more advanced topics you may refer to the following documents:

- [Translating Text](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/translating-text.md)
- [Translating Documents](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/translating-documents.md)
- [Other Functions](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/other-functions.md)
