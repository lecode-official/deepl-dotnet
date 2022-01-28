# DeepL.NET

![DeepL.NET Logo](https://raw.githubusercontent.com/lecode-official/deepl-dotnet/master/documentation/images/banner.png "DeepL.NET Logo")

[![License: MIT](https://img.shields.io/github/license/lecode-official/deepl-dotnet)](https://github.com/lecode-official/deepl-dotnet/blob/master/LICENSE)
[![Nuget Package](https://img.shields.io/nuget/v/DeepL)](https://www.nuget.org/packages/DeepL)
[![Nuget Downloads](https://img.shields.io/nuget/dt/DeepL)](https://www.nuget.org/packages/DeepL)

> **IMPORTANT** This is not an official .NET binding for DeepL and is neither created nor officially supported by DeepL. DeepL provides their own .NET client library, which can be found [here](https://github.com/DeepLcom/deepl-dotnet).

An unofficial, fully-featured .NET client for the [DeepL](https://www.deepl.com/translator) translation service. DeepL is a commercial translation service based on deep learning. This API client only supports v2 of the API as v1 has been deprecated for new DeepL API plans available from October 2018.

## Features

- .NET Core and .NET Framework compatible
- Completely asynchronous (using the async-await pattern)
- Covers the complete surface area of the DeepL API
- Translate text (including text with XML markup)
- Translate documents (Word, PowerPoint, HTML, and raw text documents)

## Getting Started

To get started you have to add the package reference to your project:

```bash
Install-Package DeepL -Version 0.4.0 # Package Manager
dotnet add package DeepL --version 0.4.0 # .NET CLI
paket add DeepL --version 0.4.0 # Paket CLI
```

or manually add a reference to your project file:

```xml
<PackageReference Include="DeepL" Version="0.4.0" />
```

Then you can start translating texts:

```csharp
using DeepL;

namespace Application
{
    public class Program
    {
        public static async Task Main(string[] arguments)
        {
            using (DeepLClient client = new DeepLClient("<authentication key>", useFreeApi: false))
            {
                try
                {
                    Translation translation = await client.TranslateAsync(
                        "This is a test sentence.",
                        Language.German
                    );
                    Console.WriteLine(translation.DetectedSourceLanguage);
                    Console.WriteLine(translation.Text);
                }
                catch (DeepLException exception)
                {
                    Console.WriteLine($"An error occurred: {exception.Message}");
                }
            }
        }
    }
}
```

For a more complex example, please refer to the [sample application](https://github.com/lecode-official/deepl-dotnet/tree/master/source/DeepL.Cli), which is a fully-featured command line tool that demonstrates the usage of all DeepL.NET features. For a complete overview of DeepL.NET, please refer to the [documentation](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/documentation.md).

## Developing

If you want to develop the project further, please install the [.NET Core SDK](https://dotnet.microsoft.com/download) and, if necessary, [Git](https://git-scm.com/downloads). After that you are ready to clone the repository and build the project:

```bash
git clone https://github.com/lecode-official/deepl-dotnet.git
cd deepl-dotnet
dotnet build
```

## Contributing

If you'd like to contribute, there are multiple ways you can help out. If you find a bug or have a feature request, please feel free to open an issue on [GitHub](https://github.com/lecode-official/deepl-dotnet/issues). If you want to contribute code, please fork the repository and use a feature branch. Pull requests are always welcome. Before forking, please open an issue where you describe what you want to do. This helps to align your ideas with mine and may prevent you from doing work, that I am already planning on doing. If you have contributed to the project, please add yourself to the contributors list (CONTRIBUTORS.md). Also, if necessary, update the [documentation](https://github.com/lecode-official/deepl-dotnet/tree/master/documentation) and the [DeepL CLI](https://github.com/lecode-official/deepl-dotnet/tree/master/source/DeepL.Cli). To help speed up the merging of your pull request, please comment and document your code extensively and try to emulate the coding style of the project.

## Releasing the NuGet Package

Before a releasing a new version to NuGet, do the following things:

- Update the changelog by adding a list of changes that were made since the last release in `CHANGELOG.md`
- Add all contributors to the `CONTRIBUTORS.md` and add the people that have contributed to the current release to the changelog
- Update the version number in the following files
  - `README.md`
  - `documentation/documentation.md`
  - `DeepL.csproj`
  - `DeepL.Sample.csproj`
- Add the the list of changes to the `<PackageReleaseNotes>` in `DeepL.csproj` (no Markdown allowed, do not indent)
- Do not forget to build the NuGet package in release configuration: `dotnet pack --configuration Release`
- Upload both the `.nupkg` and the `.snupkg` to NuGet

## License

The code in this project is licensed under MIT license. For more information see the [license file](https://github.com/lecode-official/deepl-dotnet/blob/master/LICENSE).
