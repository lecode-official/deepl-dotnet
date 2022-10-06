
#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security;
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
        /// <param name="authenticationKey">The authentication key for the DeepL API.</param>
        /// <param name="useFreeApi">Determines whether the free or pro version of the DeepL API should be used</param>
        public DeepLClient(string authenticationKey, bool useFreeApi = false)
        {
            this.authenticationKey = authenticationKey;
            this.useFreeApi = useFreeApi;

            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("User-Agent", $"DeepL.NET/{Assembly.GetExecutingAssembly().GetName().Version}");
            this.httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            this.fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
        }

        #endregion

        #region Private Static Fields

        /// <summary>
        /// Contains the DeepL API pro base URL.
        /// </summary>
        private static readonly string proApiBaseUrl = "https://api.deepl.com/v2";

        /// <summary>
        /// Contains the DeepL API Free base URL.
        /// </summary>
        private static readonly string freeApiBaseUrl = "https://api-free.deepl.com/v2";

        /// <summary>
        /// Contains the path to the action that retrieves usage statistics from the API.
        /// </summary>
        private static readonly string usageStatisticsPath = "usage";

        /// <summary>
        /// Contains the path to the action that translates a text.
        /// </summary>
        private static readonly string translatePath = "translate";

        /// <summary>
        /// Contains the path to the set of actions that translate documents (Microsoft Word documents, Microsoft PowerPoint documents, HTML
        /// documents, and plain text documents are supported).
        /// </summary>
        private static readonly string translateDocumentPath = "document";

        /// <summary>
        /// Contains the path to the action that retrieves the languages that are supported by the DeepL API.
        /// </summary>
        private static readonly string supportedLanguagesPath = "languages";

        /// <summary>
        /// Contains a map, which converts <see cref="Language"/> enumeration values to language codes for source languages.
        /// </summary>
        private static readonly Dictionary<Language, string> sourceLanguageCodeConversionMap = new Dictionary<Language, string>
        {
            [Language.Bulgarian] = "BG",
            [Language.Czech] = "CS",
            [Language.Danish] = "DA",
            [Language.German] = "DE",
            [Language.Greek] = "EL",
            [Language.English] = "EN",
            [Language.BritishEnglish] = "EN", // Region-specific variants are actually not supported, but are here to prevent errors
            [Language.AmericanEnglish] = "EN", // Region-specific variants are actually not supported, but are here to prevent errors
            [Language.Spanish] = "ES",
            [Language.Estonian] = "ET",
            [Language.Finnish] = "FI",
            [Language.French] = "FR",
            [Language.Hungarian] = "HU",
            [Language.Indonesian] = "ID",
            [Language.Italian] = "IT",
            [Language.Japanese] = "JA",
            [Language.Lithuanian] = "LT",
            [Language.Latvian] = "LV",
            [Language.Dutch] = "NL",
            [Language.Polish] = "PL",
            [Language.Portuguese] = "PT",
            [Language.BrazilianPortuguese] = "PT", // Region-specific variants are actually not supported, but are here to prevent errors
            [Language.Romanian] = "RO",
            [Language.Russian] = "RU",
            [Language.Slovak] = "SK",
            [Language.Slovenian] = "SL",
            [Language.Swedish] = "SV",
            [Language.Turkish] = "TR",
            [Language.Ukrainian] = "UK",
            [Language.Chinese] = "ZH"

        };

        /// <summary>
        /// Contains a map, which converts <see cref="Language"/> enumeration values to language codes for target languages.
        /// </summary>
        private static readonly Dictionary<Language, string> targetLanguageCodeConversionMap = new Dictionary<Language, string>
        {
            [Language.Bulgarian] = "BG",
            [Language.Czech] = "CS",
            [Language.Danish] = "DA",
            [Language.German] = "DE",
            [Language.Greek] = "EL",
            [Language.English] = "EN", // Unspecified variant for backward compatibility; please select EN-GB or EN-US instead
            [Language.BritishEnglish] = "EN-GB",
            [Language.AmericanEnglish] = "EN-US",
            [Language.Spanish] = "ES",
            [Language.Estonian] = "ET",
            [Language.Finnish] = "FI",
            [Language.French] = "FR",
            [Language.Hungarian] = "HU",
            [Language.Indonesian] = "ID",
            [Language.Italian] = "IT",
            [Language.Japanese] = "JA",
            [Language.Lithuanian] = "LT",
            [Language.Latvian] = "LV",
            [Language.Dutch] = "NL",
            [Language.Polish] = "PL",
            [Language.Portuguese] = "PT-PT",
            [Language.BrazilianPortuguese] = "PT-BR",
            [Language.Romanian] = "RO",
            [Language.Russian] = "RU",
            [Language.Slovak] = "SK",
            [Language.Slovenian] = "SL",
            [Language.Swedish] = "SV",
            [Language.Turkish] = "TR",
            [Language.Ukrainian] = "UK",
            [Language.Chinese] = "ZH"

        };

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the authentication key for the DeepL API.
        /// </summary>
        private readonly string authenticationKey;

        /// <summary>
        /// Contains a value that determines whether the free or the pro version of the DeepL API should be used.
        /// </summary>
        private readonly bool useFreeApi;

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
        /// <exception cref="ArgumentException">
        /// If the path is empty or only consists of whitespaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the path is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <returns>Returns the built URL as a string.</returns>
        private string BuildUrl(string path, IEnumerable<string> pathParameters, IDictionary<string, string> queryParameters = null)
        {
            // Validates the parameters
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The path must not be empty.");

            // Concatenates the path to the base URL
            string url = $"{(this.useFreeApi ? DeepLClient.freeApiBaseUrl : DeepLClient.proApiBaseUrl)}/{path}";

            // Adds the path parameters
            if (pathParameters != null && pathParameters.Any())
                url = string.Concat(url, "/", string.Join("/", pathParameters));

            // Adds the authentication key to the query parameters
            if (queryParameters == null)
                queryParameters = new Dictionary<string, string>();
            queryParameters.Add("auth_key", this.authenticationKey);

            // Converts the query parameters to a string and appends them to the URL
            string queryString = string.Join(
                "&",
                queryParameters.Select(keyValuePair => $"{keyValuePair.Key}={HttpUtility.HtmlEncode(keyValuePair.Value)}")
            );
            url = string.Concat(url, "?", queryString);

            // Returns the built URL
            return url;
        }

        /// <summary>
        /// Builds a URL for the DeepL API.
        /// </summary>
        /// <param name="path">The path of the URL.</param>
        /// <param name="queryParameters">The query parameters that are to be added to the URL.</param>
        /// <exception cref="ArgumentException">
        /// If the path is empty or only consists of whitespaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the path is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <returns>Returns the built URL as a string.</returns>
        private string BuildUrl(
            string path,
            IDictionary<string, string> queryParameters = null
        ) => this.BuildUrl(path, null, queryParameters);

        /// <summary>
        /// Checks the status code of the HTTP response and throws an exception if the status code represents an error.
        /// </summary>
        /// <param name="responseMessage">The HTTP response that is to be checked.</param>
        /// <exception cref="ArgumentNullException">
        /// If the response message is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
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
            // Validates the arguments
            if (responseMessage == null)
                throw new ArgumentNullException(nameof(responseMessage));

            // When the status code represents success, then nothing is done
            if (responseMessage.IsSuccessStatusCode)
                return;

            // Checks which error occurred and throws an exception accordingly
            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    try
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
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
                case HttpStatusCode.RequestUriTooLong:
                    throw new DeepLException("The request URL is too long. You can avoid this error by using a POST request instead of a GET request, and sending the parameters in the HTTP body.");
                case (HttpStatusCode)429:
                    throw new DeepLException("Too many requests. Please wait and resend your request.");
                case (HttpStatusCode)456:
                    throw new DeepLException("Quota exceeded. The character limit has been reached.");
                case HttpStatusCode.ServiceUnavailable:
                    throw new DeepLException("Resource currently unavailable. Try again later.");
                case (HttpStatusCode)529:
                    throw new DeepLException("Too many requests. Please wait and resend your request.");
                case HttpStatusCode.InternalServerError:
                    throw new DeepLException("An internal server error occurred.");
                default:
                    throw new DeepLException("An unknown error occurred.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the usage statistics of the DeepL API plan, i.e. the number of characters that may be translated and the number of
        /// characters that have been translated so far.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
        /// </exception>
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
            ).ConfigureAwait(false);
            await this.CheckResponseStatusCodeAsync(responseMessage).ConfigureAwait(false);

            // Retrieves the returned JSON and parses it into a .NET object
            string usageStatisticsContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<UsageStatistics>(usageStatisticsContent);
        }

        /// <summary>
        /// Gets the supported languages of the DeepL API (which may change over time).
        /// </summary>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurs a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. Too many requests have been made in a short period of time.
        /// 3. An internal server error has occurred.
        /// 4. The DeepL API server is unavailable.
        /// </exception>
        /// <returns>Returns a list of the supported languages.</returns>
        public async Task<IEnumerable<SupportedLanguage>> GetSupportedLanguagesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Sends a request to the DeepL API to retrieve the supported languages
            HttpResponseMessage responseMessage = await this.httpClient.GetAsync(
                this.BuildUrl(DeepLClient.supportedLanguagesPath),
                cancellationToken
            ).ConfigureAwait(false);
            await this.CheckResponseStatusCodeAsync(responseMessage).ConfigureAwait(false);

            // Retrieves the returned JSON and parses it into a .NET object
            string supportedLanguagesContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
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
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, one or more texts are <c>null</c>, or the target language code is empty or only consists of
        /// white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (texts == null)
                throw new ArgumentNullException(nameof(texts));
            if (!texts.Any())
                throw new ArgumentException("No texts were provided for translation.", nameof(texts));
            if (texts.Any(text => text == null))
                throw new ArgumentException("One or more texts are null.");
            if (targetLanguageCode == null)
                throw new ArgumentNullException(nameof(targetLanguageCode));
            if (string.IsNullOrWhiteSpace(targetLanguageCode))
                throw new ArgumentException("The target language code must not be empty or only consist of white spaces.");

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
            switch (formality)
            {
                case Formality.Default:
                    parameters.Add(new KeyValuePair<string, string>("formality", "default"));
                    break;
                case Formality.More:
                    parameters.Add(new KeyValuePair<string, string>("formality", "more"));
                    break;
                case Formality.Less:
                    parameters.Add(new KeyValuePair<string, string>("formality", "less"));
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
                ).ConfigureAwait(false);
                await this.CheckResponseStatusCodeAsync(responseMessage).ConfigureAwait(false);

                // Retrieves the returned JSON and parses it into a .NET object
                string translationResultContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<TranslationResult>(translationResultContent).Translations;
            }
        }

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source
        /// text, if possible.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, one or more texts are <c>null</c>, or the target language code is empty or only consists of
        /// white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            null,
            targetLanguageCode,
            splitting,
            preserveFormatting,
            formality,
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
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty or one or more texts are <c>null</c>, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the texts are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            DeepLClient.sourceLanguageCodeConversionMap[sourceLanguage],
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            formality,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source
        /// text, if possible.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.
        /// </param>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty or one or more texts are <c>null</c>, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the texts are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            formality,
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
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty or one or more texts are <c>null</c>, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            sourceLanguage == null ? null : sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            formality,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source
        /// text, if possible.
        /// </summary>
        /// <param name="texts">A list of texts that are to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the list of texts is empty, or one or more texts are <c>null</c>, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the texts or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            texts,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            formality,
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
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the text or the target language code are empty or only consist of white spaces, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("The text must not be empty or only consist of white spaces.");

            // Translates the text
            IEnumerable<Translation> translations = await this.TranslateAsync(
                new List<string> { text },
                sourceLanguageCode,
                targetLanguageCode,
                splitting,
                preserveFormatting,
                formality,
                xmlHandling,
                cancellationToken
            ).ConfigureAwait(false);

            // Since only one text was translated, the first translation is returned
            return translations.FirstOrDefault();
        }

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source
        /// text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the text or the target language code are empty or only consist of white spaces, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            null,
            targetLanguageCode,
            splitting,
            preserveFormatting,
            formality,
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
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the text is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the text is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            DeepLClient.sourceLanguageCodeConversionMap[sourceLanguage],
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            formality,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source
        /// text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the text is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the text is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
            formality,
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
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the text is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            sourceLanguage == null ? null : sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            formality,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source
        /// text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="xmlHandling">
        /// Determines how XML documents are handled during translation. If specified, XML handling is enabled.
        /// </param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// When the text is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// When the text or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            XmlHandling xmlHandling = default(XmlHandling),
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            targetLanguage.LanguageCode,
            splitting,
            preserveFormatting,
            formality,
            xmlHandling,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file (this file name is needed to determine the MIME type of the file).</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name or the target language code are empty or only consist of white spaces, then an <see cref="ArgumentException"/>
        /// is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public async Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("The file name must not be empty or only consist of white spaces.");
            if (targetLanguageCode == null)
                throw new ArgumentNullException(nameof(targetLanguageCode));
            if (string.IsNullOrWhiteSpace(targetLanguageCode))
                throw new ArgumentException("The target language code must not be empty or only consist of white spaces.");

            // Prepares the content of the POST request to the DeepL API
            string boundary = $"--{Guid.NewGuid().ToString()}";
            using (MultipartFormDataContent httpContent = new MultipartFormDataContent(boundary))
            {
                // Manually sets the content type, because the HTTP request message parser of the DeepL API does not seem to be standards
                // conform (by default, the System.Net.HttpClient puts the boundary string in quotes, but this causes the HTTP parser of the
                // DeepL API to not read the request properly)
                httpContent.Headers.ContentType = new MediaTypeHeaderValue($"multipart/form-data");
                httpContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));

                // Adds the file to the content of the HTTP request (again, the content disposition is set by hand, because, by  default,
                // the System.Net.HttpClient does not quote the name and file name, but the DeepL API HTTP parser does not support unquoted
                // parameter values)
                StreamContent fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                fileContent.Headers.ContentDisposition.Name = "\"file\"";
                fileContent.Headers.ContentDisposition.FileName = $"\"{Path.GetFileName(fileName)}\"";
                if (!this.fileExtensionContentTypeProvider.TryGetContentType(fileName, out string contentType))
                    contentType = "text/plain";
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                httpContent.Add(fileContent, "file", fileName);

                // Adds the parameters to the content of the HTTP request (again, the content disposition is set by hand, because, by
                // default, the System.Net.HttpClient does not quote the name, but the DeepL API HTTP parser does not support unquoted
                // parameter values)
                StringContent targetLanguageContent = new StringContent(targetLanguageCode);
                targetLanguageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                targetLanguageContent.Headers.ContentDisposition.Name = "\"target_lang\"";
                httpContent.Add(targetLanguageContent, "target_lang");
                if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
                {
                    StringContent sourceLanguageContent = new StringContent(sourceLanguageCode);
                    sourceLanguageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                    sourceLanguageContent.Headers.ContentDisposition.Name = "\"source_lang\"";
                    httpContent.Add(sourceLanguageContent, "source_lang");
                }
                string formalityValue;
                switch (formality)
                {
                    case Formality.More:
                        formalityValue = "more";
                        break;
                    case Formality.Less:
                        formalityValue = "less";
                        break;
                    default:
                        formalityValue = "default";
                        break;
                }
                StringContent formalityContent = new StringContent(formalityValue);
                formalityContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                formalityContent.Headers.ContentDisposition.Name = "\"formality\"";
                httpContent.Add(formalityContent, "formality");

                // Sends a request to the DeepL API to upload the document for translations
                HttpResponseMessage responseMessage = await this.httpClient.PostAsync(
                    this.BuildUrl(DeepLClient.translateDocumentPath),
                    httpContent,
                    cancellationToken
                ).ConfigureAwait(false);
                await this.CheckResponseStatusCodeAsync(responseMessage).ConfigureAwait(false);

                // Retrieves the returned JSON and parses it into a .NET object
                string translationResultContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<DocumentTranslation>(translationResultContent);
            }
        }

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name or the target language code are empty or only consist of white spaces, then an <see cref="ArgumentException"/>
        /// is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            string targetLanguageCode,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            null,
            targetLanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            Language sourceLanguage,
            Language targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            DeepLClient.sourceLanguageCodeConversionMap[sourceLanguage],
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            Language targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            sourceLanguage == null ? null : sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            Stream fileStream,
            string fileName,
            SupportedLanguage targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileStream,
            fileName,
            targetLanguage.LanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name or the target language code are empty, only consist of white spaces, or the file name contains one or more
        /// invalid characters, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the file, then a <see cref="SecurityException"/> is thrown.
        /// </exception>
        /// <exception name="UnauthorizedAccessException">
        /// If the file is read-only, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception name="IOException">
        /// If an I/O error occurs during the opening or reading of the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public async Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("The file name must not be empty or only consist of white spaces.");
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"The file \"{fileName}\" could not be found.");

            // Opens the file and uploads it to the DeepL API
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
            {
                return await this.UploadDocumentForTranslationAsync(
                    fileStream,
                    fileName,
                    sourceLanguageCode,
                    targetLanguageCode,
                    formality,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name or the target language code are empty, only consist of white spaces, or the file name contains one or more
        /// invalid characters, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the file, then a <see cref="SecurityException"/> is thrown.
        /// </exception>
        /// <exception name="UnauthorizedAccessException">
        /// If the file is read-only, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception name="IOException">
        /// If an I/O error occurs during the opening or reading of the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            string targetLanguageCode,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            null,
            targetLanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, or the file name contains one or more invalid characters, then an
        /// <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file name is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the file, then a <see cref="SecurityException"/> is thrown.
        /// </exception>
        /// <exception name="UnauthorizedAccessException">
        /// If the file is read-only, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception name="IOException">
        /// If an I/O error occurs during the opening or reading of the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            Language sourceLanguage,
            Language targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            DeepLClient.sourceLanguageCodeConversionMap[sourceLanguage],
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, or the file name contains one or more invalid characters, then an
        /// <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file name is <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the file, then a <see cref="SecurityException"/> is thrown.
        /// </exception>
        /// <exception name="UnauthorizedAccessException">
        /// If the file is read-only, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception name="IOException">
        /// If an I/O error occurs during the opening or reading of the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            Language targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, or the file name contains one or more invalid characters, then an
        /// <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the file, then a <see cref="SecurityException"/> is thrown.
        /// </exception>
        /// <exception name="UnauthorizedAccessException">
        /// If the file is read-only, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception name="IOException">
        /// If an I/O error occurs during the opening or reading of the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            sourceLanguage == null ? null : sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Uploads a document (Microsoft Word documents, Microsoft PowerPoint documents, HTML documents, and plain text documents are
        /// supported) for translation. The method returns immediately after the upload of the document. The translation status can be
        /// checked using <see cref="CheckTranslationStatusAsync"/>. When the translation has finished, then the translated document can be
        /// downloaded via <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/> or
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileName">The name of the file that is to be uploaded.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, or the file name contains one or more invalid characters, then an
        /// <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file name or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the file, then a <see cref="SecurityException"/> is thrown.
        /// </exception>
        /// <exception name="UnauthorizedAccessException">
        /// If the file is read-only, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception name="IOException">
        /// If an I/O error occurs during the opening or reading of the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        /// Returns an object that represents the ongoing translation of the document. This can be used to check translation status and to
        /// download the translated document.
        /// </returns>
        public Task<DocumentTranslation> UploadDocumentForTranslationAsync(
            string fileName,
            SupportedLanguage targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.UploadDocumentForTranslationAsync(
            fileName,
            targetLanguage.LanguageCode,
            formality,
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
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
                ).ConfigureAwait(false);
                await this.CheckResponseStatusCodeAsync(responseMessage).ConfigureAwait(false);

                // Retrieves the returned JSON and parses it into a .NET object
                string translationStatusContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
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
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
                ).ConfigureAwait(false);
                await this.CheckResponseStatusCodeAsync(responseMessage).ConfigureAwait(false);

                // Retrieves the returned JSON and parses it into a .NET object
                Stream stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
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
        /// <exception cref="ArgumentException">
        /// If the file name is empty, only consists of white spaces, or contains one or more invalid characters, then an
        /// <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception name="ArgumentNullException">
        /// If the document translation or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified file is read-only, or the specified file is hidden, then an
        /// <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the specified file exceeds the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the specified file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the creation of the file or while writing to the file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
        /// </exception>
        /// <exception cref="DeepLException">
        /// When an error occurred, then a <see cref="DeepLException"/> is thrown. This occurs in the following cases:
        /// 1. The authentication key is invalid.
        /// 2. The document translation does not exist anymore.
        /// 3. Too many requests have been made in a short period of time.
        /// 4. An internal server error has occurred.
        /// 5. The DeepL API server is unavailable.
        /// </exception>
        public async Task DownloadTranslatedDocumentAsync(
            DocumentTranslation documentTranslation,
            string fileName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("The file name must not be empty or only consist of white spaces.");

            // Downloads the document from the DeepL API
            using (Stream stream = await this.DownloadTranslatedDocumentAsync(documentTranslation, cancellationToken).ConfigureAwait(false))
            {
                // Writes the downloaded document to file (the CopyToAsync method does not have an overload where only a stream and a
                // cancellation token can be specified, so the one with the explicit buffer size is used; the buffer size specified here is
                // the default buffer size specified in the source of the Stream class, see
                // https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L34)
                using (FileStream fileStream = File.Create(fileName))
                    await stream.CopyToAsync(fileStream, bufferSize: 81920, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language. This method
        /// is a combination of <see cref="UploadDocumentForTranslationAsync(Stream, string, string, string, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file (this file name is needed to determine the MIME type of the file).</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name or the target language code are empty or only consist of white spaces, then an <see cref="ArgumentException"/>
        /// is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Uploads the document to the DeepL API for translation
            DocumentTranslation documentTranslation = await this.UploadDocumentForTranslationAsync(
                fileStream,
                fileName,
                sourceLanguageCode,
                targetLanguageCode,
                formality,
                cancellationToken
            ).ConfigureAwait(false);

            // Periodically checks if the document translation has finished
            while (true)
            {
                // Checks if a cancellation has been requested, if so, the process is aborted
                if (cancellationToken != null)
                    cancellationToken.ThrowIfCancellationRequested();

                // Gets the status of the translation
                TranslationStatus translationStatus = await this.CheckTranslationStatusAsync(
                    documentTranslation,
                    cancellationToken
                ).ConfigureAwait(false);

                // If the translation is done, then the loop can be exited and the translated document can be downloaded
                if (translationStatus.State == TranslationState.Done)
                    break;

                // If an error occurred during the translation, then an exception is thrown
                if (translationStatus.State == TranslationState.Error)
                    throw new DeepLException("An unknown error occurred during the translation of the document.");

                // In order to not send too many requests to the server, we wait for a short amount of time, before sending another request
                // (when the translation has started, then the translation status actually contains the estimated number of seconds
                // remaining, if they are not in the translation status, then we default to 1/2 second)
                await Task.Delay(
                    translationStatus.SecondsRemaining.HasValue ? translationStatus.SecondsRemaining.Value * 1000 : 1000,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            // Finally, since the document has now been translated, it can be downloaded
            return await this.DownloadTranslatedDocumentAsync(documentTranslation, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Translates the document in the specified stream to the specified target language. The source language is automatically inferred
        /// from the source text, if possible. This method is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(Stream, string, string, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name or the target language code are empty or only consist of white spaces, then an <see cref="ArgumentException"/>
        /// is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language code are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            null,
            targetLanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language. This method
        /// is a combination of <see cref="UploadDocumentForTranslationAsync(Stream, string, Language, Language, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            DeepLClient.sourceLanguageCodeConversionMap[sourceLanguage],
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream to the specified target language. The source language is automatically inferred
        /// from the source text, if possible. This method is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(Stream, string, Language, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language. This method
        /// is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(Stream, string, SupportedLanguage, SupportedLanguage, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the file name, or the target language are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            sourceLanguage == null ? null : sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream to the specified target language. The source language is automatically inferred
        /// from the source text, if possible. This method is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(Stream, string, SupportedLanguage, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, CancellationToken)"/>.
        /// </summary>
        /// <param name="fileStream">A stream that contains the contents of the document that is to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the file name is empty or only consists of white spaces, then an <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the file stream, the target language, or the file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            fileStream,
            fileName,
            targetLanguage.LanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language. This method
        /// is a combination of <see cref="UploadDocumentForTranslationAsync(string, string, string, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="inputFileName">The name of the file that is to be uploaded.</param>
        /// <param name="outputFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the input file name, the output file name, or the target language code are empty or only consist of white spaces, or the
        /// input file name or the output file name contain one or more invalid characters, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the input file name, the output file name, or the target language code are <c>null</c>, then an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified input file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified input file is read-only, or the specified input file is
        /// hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the input or output file exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the input or output file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the reading or writing of a file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the input or output file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the input file, then a <see cref="SecurityException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        public async Task TranslateDocumentAsync(
            string inputFileName,
            string outputFileName,
            string sourceLanguageCode,
            string targetLanguageCode,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the arguments
            if (inputFileName == null)
                throw new ArgumentNullException(nameof(inputFileName));
            if (string.IsNullOrWhiteSpace(inputFileName))
                throw new ArgumentException("The input file name must not be empty or only consist of white spaces.");
            if (outputFileName == null)
                throw new ArgumentNullException(nameof(outputFileName));
            if (string.IsNullOrWhiteSpace(outputFileName))
                throw new ArgumentException("The output file name must not be empty or only consist of white spaces.");
            if (!File.Exists(inputFileName))
                throw new FileNotFoundException($"The file \"{inputFileName}\" could not be found.");

            // Opens the file and uploads it to the DeepL API for translation and downloads the result
            using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open))
            {
                using (Stream outputFileStream = await this.TranslateDocumentAsync(
                    inputFileStream,
                    inputFileName,
                    sourceLanguageCode,
                    targetLanguageCode,
                    formality,
                    cancellationToken).ConfigureAwait(false))
                {
                    // Writes the downloaded stream to a file (the CopyToAsync method does not have an overload where only a stream and a
                    // cancellation token can be specified, so the one with the explicit buffer size is used; the buffer size specified here
                    // is the default buffer size specified in the source of the Stream class, see
                    // https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L34)
                    using (FileStream fileStream = File.Create(outputFileName))
                        await outputFileStream.CopyToAsync(fileStream, bufferSize: 81920, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Translates the document in the specified stream to the specified target language. The source language is automatically inferred
        /// from the source text, if possible. This method is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(string, string, Formality, CancellationToken)"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="inputFileName">The name of the file that is to be uploaded.</param>
        /// <param name="outputFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the input file name, the output file name, or the target language code are empty or only consist of white spaces, or the
        /// input file name or the output file name contain one or more invalid characters, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the input file name, the output file name, or the target language code are <c>null</c>, then an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified input file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified input file is read-only, or the specified input file is
        /// hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the input or output file exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the input or output file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the reading or writing of a file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the input or output file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the input file, then a <see cref="SecurityException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        public Task TranslateDocumentAsync(
            string inputFileName,
            string outputFileName,
            string targetLanguageCode,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            inputFileName,
            outputFileName,
            null,
            targetLanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language. This method
        /// is a combination of <see cref="UploadDocumentForTranslationAsync(string, Language, Language, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="inputFileName">The name of the file that is to be uploaded.</param>
        /// <param name="outputFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the input file name, or the output file name are empty or only consist of white spaces, then an
        /// <see cref="ArgumentException"/> is thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the input file name, the output file name, or the target language code are empty or only consist of white spaces, or the
        /// input file name or the output file name contain one or more invalid characters, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified input file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified input file is read-only, or the specified input file is
        /// hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the input or output file exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the input or output file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the reading or writing of a file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the input or output file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the input file, then a <see cref="SecurityException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        public Task TranslateDocumentAsync(
            string inputFileName,
            string outputFileName,
            Language sourceLanguage,
            Language targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            inputFileName,
            outputFileName,
            DeepLClient.sourceLanguageCodeConversionMap[sourceLanguage],
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream to the specified target language. The source language is automatically inferred
        /// from the source text, if possible. This method is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(string, Language, Formality, CancellationToken)"/>, <see cref="CheckTranslationStatusAsync"/>,
        /// and <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="inputFileName">The name of the file that is to be uploaded.</param>
        /// <param name="outputFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the input file name, the output file name, or the target language code are empty or only consist of white spaces, or the
        /// input file name or the output file name contain one or more invalid characters, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the input file name or the output file name are <c>null</c>, then an <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified input file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified input file is read-only, or the specified input file is
        /// hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the input or output file exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the input or output file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the reading or writing of a file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the input or output file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the input file, then a <see cref="SecurityException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        public Task TranslateDocumentAsync(
            string inputFileName,
            string outputFileName,
            Language targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            inputFileName,
            outputFileName,
            DeepLClient.targetLanguageCodeConversionMap[targetLanguage],
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream from the specified source language to the specified target language. This method
        /// is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(string, SupportedLanguage, SupportedLanguage, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="inputFileName">The name of the file that is to be uploaded.</param>
        /// <param name="outputFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the input file name, the output file name, or the target language code are empty or only consist of white spaces, or the
        /// input file name or the output file name contain one or more invalid characters, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the input file name, the output file name, or the target language are <c>null</c>, then an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified input file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified input file is read-only, or the specified input file is
        /// hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the input or output file exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the input or output file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the reading or writing of a file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the input or output file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the input file, then a <see cref="SecurityException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        public Task TranslateDocumentAsync(
            string inputFileName,
            string outputFileName,
            SupportedLanguage sourceLanguage,
            SupportedLanguage targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            inputFileName,
            outputFileName,
            sourceLanguage == null ? null : sourceLanguage.LanguageCode,
            targetLanguage.LanguageCode,
            formality,
            cancellationToken
        );

        /// <summary>
        /// Translates the document in the specified stream to the specified target language. The source language is automatically inferred
        /// from the source text, if possible. This method is a combination of
        /// <see cref="UploadDocumentForTranslationAsync(string, SupportedLanguage, Formality, CancellationToken)"/>,
        /// <see cref="CheckTranslationStatusAsync"/>, and
        /// <see cref="DownloadTranslatedDocumentAsync(DocumentTranslation, string, CancellationToken)"/>.
        /// </summary>
        /// <param name="inputFileName">The name of the file that is to be uploaded.</param>
        /// <param name="outputFileName">The name of the file into which the translated document is to be written.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="formality">Determines whether the translated text should lean towards formal or informal language.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <exception cref="ArgumentException">
        /// If the input file name, the output file name, or the target language code are empty or only consist of white spaces, or the
        /// input file name or the output file name contain one or more invalid characters, then an <see cref="ArgumentException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If the input file name, the output file name, or the target language are <c>null</c>, then an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </exception>
        /// <exception name="FileNotFoundException">
        /// If the specified input file does not exist, then a <see cref="FileNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// If the caller does not have the required permission, the specified input file is read-only, or the specified input file is
        /// hidden, then an <see cref="UnauthorizedAccessException"/> is thrown.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// If the path of the input or output file exceed the system-defined maximum length, then a <see cref="PathTooLongException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// If the path to the input or output file is invalid or does not exist, then a <see cref="DirectoryNotFoundException"/> is thrown.
        /// </exception>
        /// <exception cref="IOException">
        /// If a I/O error occurs during the reading or writing of a file, then an <see cref="IOException"/> is thrown.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the input or output file name has an invalid format, then a <see cref="NotSupportedException"/> is thrown.
        /// </exception>
        /// <exception name="SecurityException">
        /// If the caller does not have the required permission to the read the input file, then a <see cref="SecurityException"/> is
        /// thrown.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// If a cancellation of the operation has been requested, then an <see cref="OperationCanceledException"/> is thrown.
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
        public Task TranslateDocumentAsync(
            string inputFileName,
            string outputFileName,
            SupportedLanguage targetLanguage,
            Formality formality = Formality.Default,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateDocumentAsync(
            inputFileName,
            outputFileName,
            targetLanguage.LanguageCode,
            formality,
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
        /// Disposes of all the resources acquired by the <see cref="DeepLClient"/>. This method can be overridden by sub-classes to dispose
        /// of further resources.
        /// </summary>
        /// <param name="disposingManagedResources">
        /// Determines whether managed resources should be disposed of or only unmanaged resources.
        /// </param>
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
