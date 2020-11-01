# DeepL.NET

![DeepL.NET Logo](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/images/banner.png "DeepL.NET Logo")

A fully-featured .NET client for the [DeepL](https://www.deepl.com/translator) translation service. DeepL is a commercial translation service based on deep learning. This API client only supports v2 of the API as v1 has been deprecated for new DeepL API plans available from October 2018.

## Features

- .NET Core and .NET Framework compatible
- Completely asynchronous (using the async-await pattern)
- Covers the complete surface area of the DeepL API
- Translate text (including text with XML markup)
- Translate documents (Word, PowerPoint, HTML, and raw text documents)

## Getting Started

To get started you have to add the package reference to your project:

```bash
Install-Package DeepL -Version 0.1.0 # Package Manager
dotnet add package DeepL --version 0.1.0 # .NET CLI
paket add DeepL --version 0.1.0 # Paket CLI
```

or manually add a reference to your project file:

```xml
<PackageReference Include="DeepL" Version="0.1.0" />
```

Then you can start translating texts:

```csharp
using DeepL;

namespace Application
{
    public class Program
    {
        public static void Main(string[] arguments) => Program.MainAsync(arguments).Wait();

        public static async Task MainAsync(string[] arguments)
        {
            using (DeepLClient client = new DeepLClient("<authentication key>"))
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

For a more complex example, please refer to the [sample application](https://github.com/lecode-official/deepl-dotnet/tree/master/source/DeepL.Sample), which demonstrates a wide variety of features. For a complete overview of DeepL.NET, please refer to the [documentation](https://github.com/lecode-official/deepl-dotnet/blob/master/documentation/documentation.md).

## Developing

If you want to develop the project further, please install the [.NET Core SDK](https://dotnet.microsoft.com/download) and, if necessary, [Git](https://git-scm.com/downloads). After that you are ready to clone the repository and build the project:

```bash
git clone https://github.com/lecode-official/deepl-dotnet.git
cd deepl-dotnet
dotnet build
```

## Contributing

If you'd like to contribute, there are multiple ways you can help out. If you find a bug or have a feature request, please feel free to open an issue on [GitHub](https://github.com/lecode-official/deepl-dotnet/issues). If you want to contribute code, please fork the repository and use a feature branch. Pull requests are always welcome. Before forking, please open an issue where you describe what you want to do. This helps to align your ideas with mine and may prevent you from doing work, that I am already planning on doing. If you have contributed to the project, please add yourself to the contributors list (CONTRIBUTORS.md). To help speed up the merging of your pull request, please comment and document your code extensively and try to emulate the coding style of the project.

## License

The code in this project is licensed under MIT license. For more information see the [license file](https://github.com/lecode-official/deepl-dotnet/blob/master/LICENSE).
