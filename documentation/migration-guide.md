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



## Translating Text



## Translating Documents



## Listing Available Languages



## Monitoring Usage


