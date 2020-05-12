
#region Using Directives

using Newtonsoft.Json;

#endregion

namespace DeepL
{
    /// <summary>
    /// Represents a language that is supported by the DeepL API. While the <see cref="Language"/> enumeration contains the languages that
    /// are supported at the writing of this .NET implementation, a <see cref="SupportedLanguage"/> comes directly from the DeepL API itself
    /// and may therefore change over time.
    /// </summary>
    public class SupportedLanguage
    {
        #region Public Properties

        /// <summary>
        /// The language code, which can be used as a parameter for translation.
        /// </summary>
        [JsonProperty("language")]
        public string LanguageCode { get; private set; }

        /// <summary>
        /// The English name of the language.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        #endregion
    }
}
