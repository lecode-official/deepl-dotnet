# DeepL.NET Documentation

![DeepL.NET Logo](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/images/banner.png "DeepL.NET Logo")

Welcome to the documentation of DeepL.NET. DeepL.NET is a C# library for accessing the DeepL translation REST API. Please note that DeepL.NET only supports v2 of the API, as v1 is deprecated. In order to use DeepL.NET you need to have a [DeepL API subscription](https://www.deepl.com/pro.html#developer). For further information about the DeepL API, please refer to the [official documentation](https://www.deepl.com/docs-api/introduction/).

## Installation

To get started you have to add the package reference to your project. Use one of the following commands depending on your development environment (Package Manager, if you use Visual Studio, .NET CLI if you use .NET on the command line or Visual Studio Code, or Paket CLI, if you use the Paket package manager):

```bash
Install-Package DeepL -Version 0.2.1 # Package Manager
dotnet add package DeepL --version 0.2.1 # .NET CLI
paket add DeepL --version 0.2.1 # Paket CLI
```

Alternatively, you can also manually add a reference to your project file:

```xml
<PackageReference Include="DeepL" Version="0.2.1" />
```

## Getting Started

After installing the NuGet package, you can start using DeepL.NET. All functionality of DeepL.NET resides in the namespace `DeepL`, so make sure to import the namespace (`using DeepL`). The central class in the library is the `DeepLClient`, which you have to instantiate to access the DeepL API. `DeepLClient` implements `IDisposable`, so it is recommended that you either encapsulate its usage in a `using` block or implement `IDisposable` yourself. The `DeepLClient` has no internal state so a single instance can be used throughout the whole lifetime of your application. The `DeepLClient` constructor has two arguments: the authentication key for the DeepL API, which you will find in the [account settings](https://www.deepl.com/pro-account.html) after creating a DeepL API subscription and whether you want to use the free or the pro version of the DeepL API. The `useFreeApi` parameter defaults to `false`, so it can be omitted.

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
1. The authentication key is invalid
1. The resource could not be found (e.g. when the specified document translation does not exist anymore)
1. The text that is to be translated is too large (although DeepL is known to return the text untranslated in some cases)
1. Too many requests have been made in a short period of time
1. The translation quota has been exceeded
1. An internal server error has occurred
1. The DeepL API server is unavailable

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

If you want to use DeepL.NET from the your `Main` method, then create a second asynchronous `MainAsync` method like so:

```csharp
public static void Main(string[] arguments) => Program.MainAsync(arguments).Wait();

public static async Task MainAsync(string[] arguments)
{
}
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
