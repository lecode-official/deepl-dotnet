# Translating Documents

Instead of translating large volumes of text manually, the DeepL API also supports the translation of whole documents. At the time of writing this documentation (May, 2020) the following document formats are supported:

- Microsoft Word documents
- Microsoft PowerPoint documents
- HTML documents
- raw text files

Translating documents is fully asynchronous, which means that the document is first uploaded to the DeepL API, then the status of the translation can be polled, and finally, when the translation has finished, the translated document can be downloaded.

> **IMPORTANT**: Please note that, as of the time of writing this documentation (May, 2020), the document translation is still in beta and may cause problems. Please report issues to support@deepl.com.

## Uploading Documents

For uploading documents, the `DeepLClient` offers the `UploadDocumentForTranslationAsync` family of methods. These methods come in two flavors: upload the document from a `Stream` or upload the document directly from a file. When uploading from a stream, the a file name has to be specified in order to determine the MIME type of the file. The methods have the following parameters:

| Parameter | Type | Optional | Description |
|-----------|------|:--------:|-------------|
| `stream` | `Stream` | ✓ | The stream that contains the data of the document that is to be uploaded. |
| `fileName` | `string` | ✗ | The name of the file that is to be uploaded. When no stream was specified, then this must be a full qualified path to an existing file. Otherwise a simple file name with a proper extension suffices. |
| `sourceLanguageCode` or `sourceLanguage` | `string`, `Language`, or `SupportedLanguage` | ✓ | The language of the document that is to be translated. If not specified, the language is inferred from the text, if possible. |
| `targetLanguageCode` or `targetLanguage` | `string`, `Language`, or `SupportedLanguage` | ✗ | The language into which the document is to be translated. |
| `cancellationToken` | `CancellationToken` | ✓ | Can be used to cancel a long running translation. Defaults to `null`. |

All of these translation methods return an instance of `DocumentTranslation`, which contains the ID of the document (`DocumentId`) and an encryption key (`DocumentKey`). The `DocumentTranslation` must be specified when checking the translation status or when downloading the translated document.

```csharp
// Uploading a document from a file for translation
DocumentTranslation documentTranslation = await client.UploadDocumentForTranslationAsync(
    "document.docx",
    Language.German
);

// Uploads a document from a stream for translation
using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
{
    DocumentTranslation documentTranslation = this.UploadDocumentForTranslationAsync(
        fileStream,
        fileName,
        Language.German
    );
}
```

## Checking Translation Status

Using the method `CheckTranslationStatusAsync` you can check the translation status of a document uploaded using `UploadDocumentForTranslationAsync`. It takes the `DocumentTranslation` instance retrieved from the call to `UploadDocumentForTranslationAsync` as an argument and returns an instance of `TranslationStatus`, which contains, at a bare minimum the `State` and `DocumentId`. The `DocumentId` reflects the ID specified in the `DocumentTranslation`. `State` can have the following values:

| Value | Description |
|-------|-------------|
| `TranslationState.Queued` | The document was successfully uploaded and is now queued for translation. |
| `TranslationState.Translating` | The document is being translated. |
| `TranslationState.Done` | The document was translated and can now be downloaded. |
| `TranslationState.Error` | An error occurred during the translation. Try to re-upload the document. |

When the state of the translation is `TranslationState.Translating`, then the `TranslationStatus` also contains a property `SecondsRemaining`, which is the estimated number of seconds until the translation is done. Furthermore, when the translation is in the state `TranslationState.Done`, then the `TranslationStatus` contains `BilledCharacters`, which is the number of characters that were billed to your account for translating the document.

```csharp
TranslationStatus translationStatus = await this.CheckTranslationStatusAsync(documentTranslation);
```

> **IMPORTANT**: Please note that, as of the time of writing this documentation (May, 2020), when translating Microsoft Word or Microsoft PowerPoint documents, at least 50,000 characters will be billed to your account.

## Downloading Translated Documents

When the state of the `DocumentTranslation` is `TranslationState.Done`, then the translated document can be downloaded via `DownloadTranslatedDocumentAsync` method. It takes the `DocumentTranslation` as a parameter. It has two overloads: one that returns a `Stream` containing the translated document and one that expects a file name as an argument and writes the document to the specified file.

```csharp
// Downloading a translated document to a stream
Stream document = await this.DownloadTranslatedDocumentAsync(documentTranslation);

// Downloading a translated document to a file
await this.DownloadTranslatedDocumentAsync(documentTranslation, "translated.docx");
```

## All-in-One Document Translation

For your convenience, the `DeepLClient` has the `TranslateDocumentAsync` family of methods, which automatically upload a document, intelligently poll for its status, and download the document when its translation has finished. There are two types of overloads for this method: one that takes a `Stream`, a file name, an optional source language, and a target language and return a `Stream` and one that takes an input file name, an output file name, an optional source language, and a target language and saves the downloaded document to the specified output file.

```csharp
// Translating a document from a stream and downloading it into a stream
using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
{
    Stream outputStream = await this.TranslateDocumentAsync(
        fileStream,
        fileName,
        Language.German
    );
}

// Translating a document from a file and directly downloading it into another file
await client.TranslateDocumentAsync(
    "document.docx",
    "translated.docx",
    Language.German
);
```
