
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
        /// The Bulgarian language (language code "BG").
        /// </summary>
        Bulgarian,
        
        /// <summary>
        /// The Czech language (language code "CS").
        /// </summary>
        Czech,
        
        /// <summary>
        /// The Danish language (language code "DA").
        /// </summary>
        Danish,

        /// <summary>
        /// The German language (language code "DE").
        /// </summary>
        German,
        
        /// <summary>
        /// The Greek language (language code "EL").
        /// </summary>
        Greek,

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
        /// The Spanish language (language code "ES").
        /// </summary>
        Spanish,
        
        /// <summary>
        /// The Estonian language (language code "ET").
        /// </summary>
        Estonian,
        
        /// <summary>
        /// The Finnish language (language code "FI").
        /// </summary>
        Finnish,

        /// <summary>
        /// The French language (language code "FR").
        /// </summary>
        French,
        
        /// <summary>
        /// The Hungarian language (language code "HU").
        /// </summary>
        Hungarian,

        /// <summary>
        /// The Italian language (language code "IT").
        /// </summary>
        Italian,

        /// <summary>
        /// The Japanese language (language code "JA").
        /// </summary>
        Japanese,
        
        /// <summary>
        /// The Lithuanian language (language code "LT").
        /// </summary>
        Lithuanian,
        
        /// <summary>
        /// The Latvian language (language code "LV").
        /// </summary>
        Latvian,

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
        /// The Romanian language (language code "RO").
        /// </summary>
        Romanian,

        /// <summary>
        /// The Russian language (language code "RU").
        /// </summary>
        Russian,
        
        /// <summary>
        /// The Slovak language (language code "SK").
        /// </summary>
        Slovak,
        
        /// <summary>
        /// The Slovenian language (language code "SL").
        /// </summary>
        Slovenian,
        
        /// <summary>
        /// The Swedish language (language code "SV").
        /// </summary>
        Swedish,

        /// <summary>
        /// The Chinese language (language code "ZH").
        /// </summary>
        Chinese
    }
}
