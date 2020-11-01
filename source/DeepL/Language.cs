
namespace DeepL
{
    /// <summary>
    /// Represents an enumeration for the supported languages of the DeepL translation engine. This list contains all supported languages as
    /// of the time of writing this .NET implementation. In order to get the current list of supported languages use the
    /// <see cref="DeepLClient.GetSupportedLanguagesAsync"/> method.
    /// </summary>
    public enum Language
    {
        /// <summary>
        /// The German language (language code "DE").
        /// </summary>
        German,

        /// <summary>
        /// The British English language (language code "EN-GB").
        /// </summary>
        BritishEnglish,

        /// <summary>
        /// The American English language (language code "EN-US").
        /// </summary>
        AmericanEnglish,

        /// <summary>
        /// An unspecified variant of the English language (language code "EN"). This language may only be used as a target language, but
        /// not as a source language.
        /// </summary>
        English,

        /// <summary>
        /// The French language (language code "FR").
        /// </summary>
        French,

        /// <summary>
        /// The Italian language (language code "IT").
        /// </summary>
        Italian,

        /// <summary>
        /// The Japanese language (language code "JA").
        /// </summary>
        Japanese,

        /// <summary>
        /// The Spanish language (language code "ES").
        /// </summary>
        Spanish,

        /// <summary>
        /// The Dutch language (language code "NL").
        /// </summary>
        Dutch,

        /// <summary>
        /// The Polish language (language code "PL").
        /// </summary>
        Polish,

        /// <summary>
        /// The Portuguese language. When used as a source language, then this represents all Portuguese varieties mixed (langauge code
        /// "PT"). When used as a target language, then this represents all Portuguese varieties excluding Brazilian Portuguese (language
        /// code "PT-PT").
        /// </summary>
        Portuguese,

        /// <summary>
        /// The Brazilian Portuguese language (language code "PT-BR"). This language may only be used as a target language, but not as a
        /// source language.
        /// </summary>
        BrazilianPortuguese,

        /// <summary>
        /// The Russian language (language code "RU").
        /// </summary>
        Russian,

        /// <summary>
        /// The Chinese language (language code "ZH").
        /// </summary>
        Chinese
    }
}
