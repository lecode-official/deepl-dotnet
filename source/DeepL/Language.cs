
namespace DeepL
{
    /// <summary>
    /// Represents an enumeration for the supported languages of the DeepL translation engine. This list contains all
    /// supported languages as of the time of writing this .NET implementation. In order to get the current list of
    /// supported languages use the <see cref="DeepLClient.GetSupportedLanguagesAsync"/> method.
    /// </summary>
    public enum Language
    {
        /// <summary>
        /// The German language (language code "DE").
        /// </summary>
        German,

        /// <summary>
        /// The English language (language code "EN").
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
        /// The Portuguese language (all Portuguese varieties excluding Brazilian Portuguese, language code "PT").
        /// </summary>
        Portuguese,

        /// <summary>
        /// The Portuguese (Brazilian) language (language code "PT-BR"). This language may only be used as a target
        /// language, but not as a source language.
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
