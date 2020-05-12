
#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents a client for communicating with the DeepL API.
    /// </summary>
    public class DeepLClient : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="DeepLClient"/> instance.
        /// </summary>
        /// <param name="authenticationKey">he authentication key for the DeepL API.</param>
        public DeepLClient(string authenticationKey)
        {
            this.authenticationKey = authenticationKey;

            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", "DeepL.NET/0.1");
            this.httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            this.fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
        }

        #endregion

        #region Private Static Fields

        /// <summary>
        /// Contains the DeepL API base URL.
        /// </summary>
        private static readonly string baseUrl = "https://api.deepl.com/v2";

        /// <summary>
        /// Contains the path to the action that retrieves usage statistics from the API.
        /// </summary>
        private static readonly string usageStatisticsPath = "usage";

        /// <summary>
        /// Contains the path to the action that translates a text.
        /// </summary>
        private static readonly string translatePath = "translate";

        /// <summary>
        /// Contains the path to the action that translates documents (Microsoft Word documents, Microsoft PowerPoint
        /// documents, HTML documents, and plain text documents are supported).
        /// </summary>
        private static readonly string translateDocumentPath = "document";

        /// <summary>
        /// Contains the path to the action that retrieves the langauges that are supported by the DeepL API.
        /// </summary>
        private static readonly string supportedLanguagesPath = "languages";

        /// <summary>
        /// Contains a map, which converts languages enumeration values to language codes.
        /// </summary>
        private static readonly Dictionary<Language, string> languageCodeConversionMap = new Dictionary<Language, string>
        {
            [Language.German] = "DE",
            [Language.English] = "EN",
            [Language.French] = "FR",
            [Language.Italian] = "IT",
            [Language.Japanese] = "JA",
            [Language.Spanish] = "ES",
            [Language.Dutch] = "NL",
            [Language.Polish] = "PL",
            [Language.Portuguese] = "PT",
            [Language.BrazilianPortuguese] = "PT-BR",
            [Language.Russian] = "RU",
            [Language.Chinese] = "ZH",
        };

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the authentication key for the DeepL API.
        /// </summary>
        private readonly string authenticationKey;

        /// <summary>
        /// Contains an HTTP client, which is used to call the DeepL API.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// Contains a provider, which maps file names to content (MIME) types.
        /// </summary>
        private readonly FileExtensionContentTypeProvider fileExtensionContentTypeProvider;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value that determines whether the <see cref="DeepLClient"/> has already been disposed of.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds a URL for the DeepL API.
        /// </summary>
        /// <param name="path">The path of the URL.</param>
        /// <param name="pathParameters">The parameters that are added to the path of the URL.</param>
        /// <param name="queryParameters">The query parameters that are to be added to the URL.</param>
        /// <returns>Returns the built URL as a string.</returns>
        private string BuildUrl(string path, IEnumerable<string> pathParameters, IDictionary<string, string> queryParameters = null)
        {
            // Concatenates the path to the base URL
            string url = $"{DeepLClient.baseUrl}/{path}";

            // Adds the path parameters
            if (pathParameters != null && pathParameters.Any())
                url = string.Concat(url, "/", string.Join("/", pathParameters));

            // When no query parameters were passed to the method, then a new dictionary of parameters is created
            if (queryParameters == null)
                queryParameters = new Dictionary<string, string>();

            // Adds the authentication key to the query parameters
            queryParameters.Add("auth_key", this.authenticationKey);

            // Converts the query parameters to a string and appends them to the URL
            string queryString = string.Join("&", queryParameters.Select(keyValuePair => $"{keyValuePair.Key}={HttpUtility.HtmlEncode(keyValuePair.Value)}"));
            url = string.Concat(url, "?", queryString);

            // Returns the built URL
            return url;
        }

        /// <summary>
        /// Builds a URL for the DeepL API.
        /// </summary>
        /// <param name="path">The path of the URL.</param>
        /// <param name="queryParameters">The query parameters that are to be added to the URL.</param>
        /// <returns>Returns the built URL as a string.</returns>
        private string BuildUrl(string path, IDictionary<string, string> queryParameters = null) => this.BuildUrl(path, null, queryParameters);

        /// <summary>
        /// Checks the status code of the HTTP response and throws an exception if the status code represents an error.
        /// </summary>
        /// <param name="responseMessage">The HTTP response that is to be checked.</param>
        /// <exception cref="DeepLException">
        /// When the status code represents an error, then a <see cref="DeepLException"/> is thrown. This occurs in the
        /// following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The resource could not be found (e.g. when the specified document translation does not exist anymore).
        /// 4. The text that is to be translated is too large.
        /// 5. Too many requests have been made in a short period of time.
        /// 6. The translation quota has been exceeded.
        /// 7. An internal server error has occurred.
        /// 8. The DeepL API server is unavailable.
        /// </exception>
        private async Task CheckResponseStatusCodeAsync(HttpResponseMessage responseMessage)
        {
            // When the status code represents success, then nothing is done
            if (responseMessage.IsSuccessStatusCode)
                return;

            // Checks which error occurred and throws an exception accordingly
            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    try
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync();
                        ErrorResult errorResult = JsonConvert.DeserializeObject<ErrorResult>(content);
                        throw new DeepLException($"Bad request. Please check error message and your parameters. {errorResult.Message}");
                    }
                    catch (JsonReaderException) { }
                    throw new DeepLException("Bad request. Please check error message and your parameters.");
                case HttpStatusCode.Forbidden:
                    throw new DeepLException("Authorization failed. Please supply a valid authentication key.");
                case HttpStatusCode.NotFound:
                    throw new DeepLException("The requested resource could not be found.");
                case HttpStatusCode.RequestEntityTooLarge:
                    throw new DeepLException("The request size exceeds the limit.");
                case (HttpStatusCode)429:
                    throw new DeepLException("Too many requests. Please wait and resend your request.");
                case (HttpStatusCode)456:
                    throw new DeepLException("Quota exceeded. The character limit has been reached.");
                case HttpStatusCode.InternalServerError:
                    throw new DeepLException("An internal server error occurred.");
                case HttpStatusCode.ServiceUnavailable:
                    throw new DeepLException("Resource currently unavailable. Try again later.");
                default:
                    throw new DeepLException("An unknown error occurred.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the usage statistics of the DeepL API plan, i.e. the number of characters that may be translated and the number of characters that have been translated so far.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. Too many requests have been made in a short period of time.
        /// 3. An internal server error has occurred.
        /// 4. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the usage statistics of the DeepL API plan.</returns>
        public async Task<UsageStatistics> GetUsageStatisticsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Sends a request to the DeepL API to retrieve the usage statistics
            HttpResponseMessage responseMessage = await this.httpClient.GetAsync(
                this.BuildUrl(DeepLClient.usageStatisticsPath),
                cancellationToken
            );
            await this.CheckResponseStatusCodeAsync(responseMessage);

            // Retrieves the returned JSON and parses it into a .NET object
            string usageStatisticsContent = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UsageStatistics>(usageStatisticsContent);
        }

        /// <summary>
        /// Gets the supported languages of the DeepL API (which may change over time).
        /// </summary>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. Too many requests have been made in a short period of time.
        /// 3. An internal server error has occurred.
        /// 4. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a list of the supported languages.</returns>
        public async Task<IEnumerable<SupportedLanguage>> GetSupportedLanguagesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Sends a request to the DeepL API to retrieve the supported languages
            HttpResponseMessage responseMessage = await this.httpClient.GetAsync(
                this.BuildUrl(DeepLClient.supportedLanguagesPath),
                cancellationToken
            );
            await this.CheckResponseStatusCodeAsync(responseMessage);

            // Retrieves the returned JSON and parses it into a .NET object
            string supportedLanguagesContent = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<SupportedLanguage>>(supportedLanguagesContent);
        }

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translations.</returns>
        public async Task<IEnumerable<Translation>> TranslateAsync(
            IEnumerable<string> texts,
            string sourceLanguageCode,
            string targetLanguageCode,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (texts == null)
                throw new ArgumentNullException(nameof(texts));
            if (!texts.Any())
                throw new ArgumentException("No texts were provided for translation.", nameof(texts));
            if (string.IsNullOrWhiteSpace(targetLanguageCode))
                throw new ArgumentNullException(nameof(targetLanguageCode));

            // Prepares the parameters for the HTTP POST request
            List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
            foreach (string text in texts)
                parameters.Add(new KeyValuePair<string, string>("text", text));
            if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
                parameters.Add(new KeyValuePair<string, string>("source_lang", sourceLanguageCode));
            parameters.Add(new KeyValuePair<string, string>("target_lang", targetLanguageCode));
            switch (splitting)
            {
                case Splitting.None:
                    parameters.Add(new KeyValuePair<string, string>("split_sentences", "0"));
                    break;
                case Splitting.InterpunctionAndNewLines:
                    parameters.Add(new KeyValuePair<string, string>("split_sentences", "1"));
                    break;
                case Splitting.Interpunction:
                    parameters.Add(new KeyValuePair<string, string>("split_sentences", "nonewlines"));
                    break;
            }
            parameters.Add(new KeyValuePair<string, string>("preserve_formatting", preserveFormatting ? "1" : "0"));
            if (xmlHandling != null)
            {
                parameters.Add(new KeyValuePair<string, string>("tag_handling", "xml"));
                if (xmlHandling.NonSplittingTags != null && xmlHandling.NonSplittingTags.Any())
                    parameters.Add(new KeyValuePair<string, string>("non_splitting_tags", string.Join(",", xmlHandling.NonSplittingTags)));
                if (xmlHandling.SplittingTags != null && xmlHandling.SplittingTags.Any())
                    parameters.Add(new KeyValuePair<string, string>("splitting_tags", string.Join(",", xmlHandling.SplittingTags)));
                if (xmlHandling.IgnoreTags != null && xmlHandling.IgnoreTags.Any())
                    parameters.Add(new KeyValuePair<string, string>("ignore_tags", string.Join(",", xmlHandling.IgnoreTags)));
                parameters.Add(new KeyValuePair<string, string>("outline_detection", xmlHandling.OutlineDetection ? "1" : "0"));
            }

            // Sends a request to the DeepL API to translate the text
            using (HttpContent httpContent = new FormUrlEncodedContent(parameters))
            {
                HttpResponseMessage responseMessage = await this.httpClient.PostAsync(
                    this.BuildUrl(DeepLClient.translatePath),
                    httpContent,
                    cancellationToken
                );
                await this.CheckResponseStatusCodeAsync(responseMessage);

                // Retrieves the returned JSON and parses it into a .NET object
                string translationResultContent = await responseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TranslationResult>(translationResultContent).Translations;
            }
        }

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            IEnumerable<string> texts,
            string targetLanguageCode,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            null,
            targetLanguageCode,
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the texts are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            IEnumerable<string> texts,
            Language sourceLanguage,
            Language targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the texts are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            IEnumerable<string> texts,
            Language targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the texts, the source language, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            IEnumerable<string> texts,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            IEnumerable<string> texts,
            SupportedLanguage targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translation.</returns>
        public async Task<Translation> TranslateAsync(
            string text,
            string sourceLanguageCode,
            string targetLanguageCode,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException(nameof(text));

            // Translates the text
            IEnumerable<Translation> translations = await this.TranslateAsync(
                new List<string> { text },
                sourceLanguageCode,
                targetLanguageCode,
                splitting,
                preserveFormatting,
                xmlHandling,
                cancellationToken
            );

            // Since only one text was translated, the first translation is returned
            return translations.FirstOrDefault();
        }

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translation.</returns>
        public Task<Translation> TranslateAsync(
            string text,
            string targetLanguageCode,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            null,
            targetLanguageCode,
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the text is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translation.</returns>
        public Task<Translation> TranslateAsync(
            string text,
            Language sourceLanguage,
            Language targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the text is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translation.</returns>
        public Task<Translation> TranslateAsync(
            string text,
            Language targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the text, the source language, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translation.</returns>
        public Task<Translation> TranslateAsync(
            string text,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="xmlHandling">Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. The text that is to be translated is too large.
        /// 4. Too many requests have been made in a short period of time.
        /// 5. The translation quota has been exceeded.
        /// 6. An internal server error has occurred.
        /// 7. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the translation.</returns>
        public Task<Translation> TranslateAsync(
            string text,
            SupportedLanguage targetLanguage,
            Splitting splitting = Splitting.InterpunctionAndNewLines,
            bool preserveFormatting = false,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file (this file name is needed to determine the MIME type of the file).</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public async Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(targetLanguageCode))
                throw new ArgumentNullException(nameof(targetLanguageCode));

            // Prepares the content of the POST request to the DeepL API
            string boundary = $"--{Guid.NewGuid().ToString()}";
            using (MultipartFormDataContent httpContent = new MultipartFormDataContent(boundary))
            {
                // Manually sets the content type, because the HTTP request message parser of the DeepL API does not seem to be
                // standards conform (by default, the System.Net.HttpClient puts the boundary string in quotes, but this causes
                // the HTTP parser of the DeepL API to not read the request properly)
                httpContent.Headers.ContentType = new MediaTypeHeaderValue($"multipart/form-data");
                httpContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));

                // Adds the file to the content of the HTTP request (again, the content disposition is set by hand, because, by
                // default, the System.Net.HttpClient does not quote the name and file name, but the DeepL API HTTP parser does
                // not support unquoted parameter values)
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                fileContent.Headers.ContentDisposition.Name = "\"file\"";
                fileContent.Headers.ContentDisposition.FileName = $"\"{fileName}\"";
                if (!this.fileExtensionContentTypeProvider.TryGetContentType(fileName, out string contentType))
                    contentType = "text/plain";
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                Console.WriteLine(contentType);
                httpContent.Add(fileContent, "file", fileName);

                // Adds the parameters to the content of the HTTP request (again, the content disposition is set by hand, because,
                // by default, the System.Net.HttpClient does not quote the name, but the DeepL API HTTP parser does not support
                // unquoted parameter values)
                var targetLanguageContent = new StringContent(targetLanguageCode);
                targetLanguageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                targetLanguageContent.Headers.ContentDisposition.Name = "\"target_lang\"";
                httpContent.Add(targetLanguageContent, "target_lang");
                if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
                    httpContent.Add(new StringContent(sourceLanguageCode), "source_lang");

                // Sends a request to the DeepL API to upload the document for translations
                HttpResponseMessage responseMessage = await this.httpClient.PostAsync(
                    this.BuildUrl(DeepLClient.translateDocumentPath),
                    httpContent,
                    cancellationToken
                );
                await this.CheckResponseStatusCodeAsync(responseMessage);

                // Retrieves the returned JSON and parses it into a .NET object
                string translationResultContent = await responseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DocumentTranslation>(translationResultContent);
            }
        }

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            null,
            targetLanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            Language sourceLanguage,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, the source language, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public async Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"The file \"{fileName}\" could not be found.");

            // Opens the file and uploads it to the DeepL API
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
            {
                return await this.UploadDocumentForTranslationAsync(
                    fileStream,
                    Path.GetFileName(fileName),
                    sourceLanguageCode,
                    targetLanguageCode,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            null,
            targetLanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file name is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            Language sourceLanguage,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file name is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file name, the source language, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents
        /// are supported) for translation. The method returns immediately after the upload of the document. The translation status
        /// can be checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated
        /// document can be downloaded via <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status
        /// and to download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Determines the status of the ongoing document translation.
        /// </summary>
        /// <param name="documentTranslation">The ongoing translation of the document.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception name="ArgumentNullException">
        /// If the document translation is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurred, then a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. The document translation does not exist anymore.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. An internal server error has occurred.
        /// 5. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns the status of the ongoing translation of the document.</returns>
        public async Task<TranslationStatus> CheckTranslationStatusAsync(
            DocumentTranslation documentTranslation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (documentTranslation == null)
                throw new ArgumentNullException(nameof(documentTranslation));

            // Prepares the parameters for the HTTP POST request
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                ["document_key"] = documentTranslation.DocumentKey
            };

            // Sends a request to the DeepL API to retrieve the status of the translation of the document
            using (HttpContent httpContent = new FormUrlEncodedContent(parameters))
            {
                HttpResponseMessage responseMessage = await this.httpClient.PostAsync(
                    this.BuildUrl(DeepLClient.translateDocumentPath, new List<string> { documentTranslation.DocumentId }),
                    httpContent,
                    cancellationToken
                );
                await this.CheckResponseStatusCodeAsync(responseMessage);

                // Retrieves the returned JSON and parses it into a .NET object
                string translationStatusContent = await responseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TranslationStatus>(translationStatusContent);
            }
        }

        /// <summary>
        /// Downloads the translated document from the DeepL API. The translation process of the document must be finished.
        /// </summary>
        /// <param name="documentTranslation">The ongoing translation of the document.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception name="ArgumentNullException">
        /// If the document translation is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurred, then a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. The document translation does not exist anymore.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. An internal server error has occurred.
        /// 5. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public async Task<Stream> DownloadTranslatedDocumentAsync(
            DocumentTranslation documentTranslation,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (documentTranslation == null)
                throw new ArgumentNullException(nameof(documentTranslation));

            // Prepares the parameters for the HTTP POST request
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                ["document_key"] = documentTranslation.DocumentKey
            };

            // Sends a request to the DeepL API to download the translated document
            using (HttpContent httpContent = new FormUrlEncodedContent(parameters))
            {
                HttpResponseMessage responseMessage = await this.httpClient.PostAsync(
                    this.BuildUrl(DeepLClient.translateDocumentPath, new List<string> { documentTranslation.DocumentId, "result" }),
                    httpContent,
                    cancellationToken
                );
                await this.CheckResponseStatusCodeAsync(responseMessage);

                // Retrieves the returned JSON and parses it into a .NET object
                Stream stream = await responseMessage.Content.ReadAsStreamAsync();
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }

        /// <summary>
        /// Downloads the translated document from the DeepL API. The translation process of the document must be finished.
        /// </summary>
        /// <param name="documentTranslation">The ongoing translation of the document.</param>
        /// <param name="fileName">The name of the file to which the downloaded document is to be written.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception name="ArgumentNullException">
        /// If the document translation or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurred, then a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. The document translation does not exist anymore.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. An internal server error has occurred.
        /// 5. The DeepL API server is unavailable.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        public async Task DownloadTranslatedDocumentAsync(
            DocumentTranslation documentTranslation,
            string fileName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            // Downloads the document from the DeepL API
            using (Stream stream = await this.DownloadTranslatedDocumentAsync(documentTranslation, cancellationToken))
            {
                // Writes the downloaded document to file
                using (FileStream fileStream = File.Create(fileName))
                    await stream.CopyToAsync(fileStream);
            }
        }

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file (this file name is needed to determine the MIME type of the file).</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public async Task<Stream> TranslateDocumentAsync(
            Stream fileStream,
            string fileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Uploads the document to the DeepL API for translation
            DocumentTranslation documentTranslation = await this.UploadDocumentForTranslationAsync(
                fileStream,
                fileName,
                sourceLanguageCode,
                targetLanguageCode,
                cancellationToken
            );

            // Periodically checks if the document translation has finished
            while (true)
            {
                // Checks if a cancellation has been requested, if so, the process is aborted
                if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                    return null;

                // Gets the status of the translation
                TranslationStatus translationStatus = await this.CheckTranslationStatusAsync(
                    documentTranslation,
                    cancellationToken
                );

                // If the translation is done, then the loop can be exited and the translated document can be downloaded
                if (translationStatus.Status == TranslationState.Done)
                    break;

                // In order to not send too many requests to the server, we wait for a short amount of time, before sending
                // another request (when the tranlation has started, then the translation status actually contains the estimated
                // number of seconds remaining, if they are not in the translation status, then we default to 1/2 second)
                await Task.Delay(
                    translationStatus.SecondsRemaining.HasValue ? translationStatus.SecondsRemaining.Value * 1000 : 1000,
                    cancellationToken
                );
            }

            // Finally, since the document has now been translated, it can be downloaded
            return await this.DownloadTranslatedDocumentAsync(documentTranslation, cancellationToken);
        }

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public Task<Stream> TranslateDocumentAsync(
            Stream fileStream,
            string fileName,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            null,
            targetLanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public Task<Stream> TranslateDocumentAsync(
            Stream fileStream,
            string fileName,
            Language sourceLanguage,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public Task<Stream> TranslateDocumentAsync(
            Stream fileStream,
            string fileName,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, the source language, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public Task<Stream> TranslateDocumentAsync(
            Stream fileStream,
            string fileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the target language, or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a stream, that contains the contents of the translated document.</returns>
        public Task<Stream> TranslateDocumentAsync(
            Stream fileStream,
            string fileName,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="sourceFileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the source file name, the target file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified source file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        public async Task TranslateDocumentAsync(
            string sourceFileName,
            string targetFileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (string.IsNullOrWhiteSpace(sourceFileName))
                throw new ArgumentNullException(nameof(sourceFileName));
            if (string.IsNullOrWhiteSpace(targetFileName))
                throw new ArgumentNullException(nameof(targetFileName));
            if (!File.Exists(sourceFileName))
                throw new FileNotFoundException($"The file \"{sourceFileName}\" could not be found.");

            // Opens the file and uploads it to the DeepL API for translation and downloads the result
            using (FileStream inputFileStream = new FileStream(sourceFileName, FileMode.Open))
            {
                using (Stream outputFileStream = await this.TranslateDocumentAsync(inputFileStream, Path.GetFileName(sourceFileName), sourceLanguageCode, targetLanguageCode, cancellationToken))
                {
                    using (FileStream fileStream = File.Create(targetFileName))
                        await outputFileStream.CopyToAsync(fileStream);
                }
            }
        }

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="sourceFileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the source file name, the target file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified source file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        public Task TranslateDocumentAsync(
            string sourceFileName,
            string targetFileName,
            string targetLanguageCode,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            sourceFileName,
            targetFileName,
            null,
            targetLanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="sourceFileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the source file name or the target file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified source file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        public Task TranslateDocumentAsync(
            string sourceFileName,
            string targetFileName,
            Language sourceLanguage,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            sourceFileName,
            targetFileName,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="sourceFileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the source file name or the target file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified source file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        public Task TranslateDocumentAsync(
            string sourceFileName,
            string targetFileName,
            Language targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            sourceFileName,
            targetFileName,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="sourceFileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the source file name, the target file name, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified source file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        public Task TranslateDocumentAsync(
            string sourceFileName,
            string targetFileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            sourceFileName,
            targetFileName,
            sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language.
        /// This method is a combination of <see cref="UploadDocumentForTranslationAsync"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync"/>.
        /// </summary>
        /// <param name="sourceFileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentNullException">
        /// If the source file name, the target file name, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">If the specified source file does not exist, then a <see cref="FileNotFoundException"/> is thrown.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.</exception>
        /// <exception cref="ArgumentException">If the file name contains one or more invalid characters, then an <see cref="ArgumentException"/> is thrown.</exception>
        /// <exception cref="PathTooLongException">If the path of the specified file is exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.</exception>
        /// <exception cref="DirectoryNotFoundException">If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.</exception>
        /// <exception cref="IOException">If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.</exception>
        /// <exception cref="NotSupportedException">If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.</exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The parameters are invalid (e.g. the source or target language are not supported).
        /// 2. The authentication key is invalid.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. The translation quota has been exceeded.
        /// 5. An internal server error has occurred.
        /// 6. The DeepL API server is unavailable.
        /// </exception>
        public Task TranslateDocumentAsync(
            string sourceFileName,
            string targetFileName,
            SupportedLanguage targetLanguage,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            sourceFileName,
            targetFileName,
            targetLanguage.LanguageCode,
            cancellationToken
        );

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the resources acquired by the <see cref="DeepLClient"/>.
        /// </summary>
        public void Dispose()
        {
            // Calls the dispose method, which can be overridden by sub-classes to dispose of further resources
            this.Dispose(true);

            // Suppresses the finalization of this object by the garbage collector, because the resources have already been disposed of
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of all the resources acquired by the <see cref="DeepLClient"/>. This method can be overridden by sub-classes to dispose of further resources.
        /// </summary>
        /// <param name="disposingManagedResources">Determines whether managed resources should be disposed of or only unmanaged resources.</param>
        protected virtual void Dispose(bool disposingManagedResources)
        {
            // Checks if the DeepL API client has already been disposed of
            if (this.IsDisposed)
                throw new ObjectDisposedException("The DeepL API client has already been disposed of.");
            this.IsDisposed = true;

            // Checks if unmanaged resources should be disposed of
            if (disposingManagedResources)
            {
                // Checks if the HTTP client has already been disposed of, if not then it is disposed of
                if (this.httpClient != null)
                    this.httpClient.Dispose();
            }
        }

        #endregion
    }
}
