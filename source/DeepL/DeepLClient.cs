
#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        /// <param name="queryParameters">The query parameters that are to be added to the URL</param>
        /// <returns>Returns the built URL as a string.</returns>
        private string BuildUrl(string path, Dictionary<string, string> queryParameters = null)
        {
            // Concatenates the path to the base URL
            string uri = $"{DeepLClient.baseUrl}/{path}";

            // When no query parameters were passed to the method, then a new dictionary of parameters is created
            if (queryParameters == null)
                queryParameters = new Dictionary<string, string>();

            // Adds the authentication key to the query parameters
            queryParameters.Add("auth_key", this.authenticationKey);

            // Converts the query parameters to a string and appends them to the URL
            string queryString = string.Join("&", queryParameters.Select(keyValuePair => $"{keyValuePair.Key}={HttpUtility.HtmlEncode(keyValuePair.Value)}"));
            uri = string.Concat(uri, "?", queryString);

            // Returns the built URL
            return uri;
        }

        /// <summary>
        /// Checks the status code of the HTTP response and throws an exception if the status code represents an error.
        /// </summary>
        /// <param name="responseMessage">The HTTP response that is to be checked.</param>
        /// <exception cref="DeepLException">When the status code represents an error, then a <see cref="DeepLException"/> is thrown.</exception>
        private void CheckResponseStatusCode(HttpResponseMessage responseMessage)
        {
            // When the status code represents success, then nothing is done
            if (responseMessage.IsSuccessStatusCode)
                return;

            // Checks which error occurred and throws an exception accordingly
            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
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
        /// <returns>Returns the usage statistics of the DeepL API plan.</returns>
        public async Task<UsageStatistics> GetUsageStatisticsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Sends a request to the DeepL API to retrieve the usage statistics
            HttpResponseMessage responseMessage = await this.httpClient.GetAsync(
                this.BuildUrl(DeepLClient.usageStatisticsPath),
                cancellationToken
            );
            this.CheckResponseStatusCode(responseMessage);

            // Retrieves the returned JSON and parses it into a .NET object
            string usageStatisticsContent = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UsageStatistics>(usageStatisticsContent);
        }

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="sourceLanguageCode">The source language code.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <returns>Returns the translations.</returns>
        public async Task<IEnumerable<Translation>> TranslateAsync(
            string text,
            string sourceLanguageCode,
            string targetLanguageCode,
            Splitting splitting = Splitting.None,
            bool preserveFormatting = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Validates the parameters
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException(nameof(text));
            if (string.IsNullOrWhiteSpace(targetLanguageCode))
                throw new ArgumentNullException(nameof(targetLanguageCode));
            if (sourceLanguageCode == "PT-BR")
                throw new DeepLException("The Portuguese (Brazilian) language must not be used as a source language.");

            // Prepares the intput as POST parameters
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("text", text);
            if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
                parameters.Add("source_lang", sourceLanguageCode);
            parameters.Add("target_lang", targetLanguageCode);
            switch (splitting)
            {
                case Splitting.None:
                    parameters.Add("split_sentences", "0");
                    break;
                case Splitting.InterpunctionAndNewLines:
                    parameters.Add("split_sentences", "1");
                    break;
                case Splitting.Interpunction:
                    parameters.Add("split_sentences", "nonewlines");
                    break;
            }
            parameters.Add("preserve_formatting", preserveFormatting ? "1" : "0");

            // Sends a request to the DeepL API to translate the text
            HttpResponseMessage responseMessage = await this.httpClient.PostAsync(
                this.BuildUrl(DeepLClient.translatePath),
                new FormUrlEncodedContent(parameters),
                cancellationToken
            );
            this.CheckResponseStatusCode(responseMessage);

            // Retrieves the returned JSON and parses it into a .NET object
            string translationResultContent = await responseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TranslationResult>(translationResultContent).Translations;
        }

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguageCode">The target language code.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            string text,
            string targetLanguageCode,
            Splitting splitting = Splitting.None,
            bool preserveFormatting = false,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(text, null, targetLanguageCode, splitting, preserveFormatting, cancellationToken);

        /// <summary>
        /// Translates the specified text to the specified target language. The source language is automatically inferred from the source text, if possible.
        /// </summary>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <param name="splitting">Determines if and how the text is to be split.</param>
        /// <param name="preserveFormatting">Determines if the formatting of the source text is to be preserved.</param>
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            string text,
            Language sourceLanguage,
            Language targetLanguage,
            Splitting splitting = Splitting.None,
            bool preserveFormatting = false,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            DeepLClient.languageCodeConversionMap[sourceLanguage],
            DeepLClient.languageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
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
        /// <param name="cancellationToken">A cancellation token, that can be used to cancel the request to the DeepL API.</param>
        /// <returns>Returns the translations.</returns>
        public Task<IEnumerable<Translation>> TranslateAsync(
            string text,
            Language targetLanguage,
            Splitting splitting = Splitting.None,
            bool preserveFormatting = false,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => this.TranslateAsync(
            text,
            DeepLClient.languageCodeConversionMap[targetLanguage],
            splitting,
            preserveFormatting,
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
