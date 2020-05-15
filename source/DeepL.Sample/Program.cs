
#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#endregion

namespace DeepL.Sample
{
    /// <summary>
    /// Represents the DeepL test application, which showcases the usage of the DeepL.NET library.
    /// </summary>
    public class Program
    {
        #region Public Static Methods

        /// <summary>
        /// The entry point to the DeepL test application.
        /// </summary>
        /// <param name="arguments">The command line arguments, which should be empty, because they are not used.</param>
        public static void Main(string[] arguments) => Program.MainAsync(arguments).Wait();

        /// <summary>
        /// Represents the asynchronous entry point to the application, which makes it possible to call asynchronous methods.
        /// </summary>
        /// <param name="arguments">The command line arguments, which should be empty, because they are not used.</param>
        public static async Task MainAsync(string[] arguments)
        {
            // Checks if the user specified any arguments, if not, then the usage of the application is printed out
            if (!arguments.Any())
                Program.WriteUsageAndExit();

            // Checks if the user wants to see the version of the application
            if (arguments.Contains("--version"))
                Program.WriteVersionAndExit();

            // Checks if the user requested help
            if (arguments.Contains("--help") || arguments.Contains("-h"))
            {
                if (Program.availableCommands.Contains(arguments.First()))
                    Program.WriteHelpAndExit(arguments.First());
                else
                    Program.WriteHelpAndExit();
            }

            // Checks if the specified command exists
            if (!Program.availableCommands.Contains(arguments.First()))
            {
                Console.WriteLine($"The command {arguments.First()} is invalid.");
                Environment.Exit(1);
            }

            // Executes the command specified by the user
            try
            {
                if (arguments.First() == "translate")
                {
                    if (arguments.Length == 4)
                    {
                        await Program.TranslateAsync(arguments[1], arguments[2], null, arguments[3]);
                    }
                    else if (arguments.Length == 5)
                    {
                        await Program.TranslateAsync(arguments[1], arguments[2], arguments[3], arguments[4]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid number of arguments for command translate.");
                        Environment.Exit(1);
                    }
                }
                else if (arguments.First() == "translate-document")
                {
                    if (arguments.Length == 5)
                    {
                        await Program.TranslateDocumentAsync(arguments[1], arguments[2], arguments[3], null, arguments[4]);
                    }
                    else if (arguments.Length == 6)
                    {
                        await Program.TranslateDocumentAsync(arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid number of arguments for command translate-document.");
                        Environment.Exit(1);
                    }
                }
                else if (arguments.First() == "get-usage-statistics")
                {
                    if (arguments.Length == 2)
                    {
                        await Program.GetUsageStatisticsAsync(arguments[1]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid number of arguments for command get-usage-statistics.");
                        Environment.Exit(1);
                    }
                }
                else if (arguments.First() == "get-supported-languages")
                {
                    if (arguments.Length == 2)
                    {
                        await Program.GetSupportedLanguagesAsync(arguments[1]);
                    }
                    else
                    {
                        Console.WriteLine("Invalid number of arguments for command get-supported-languages.");
                        Environment.Exit(1);
                    }
                }
            }
            catch (DeepLException exception)
            {
                Console.WriteLine($"An error occurred: {exception.Message}");
                Environment.Exit(1);
            }
        }

        #endregion

        #region Private Static Fields

        /// <summary>
        /// Contains a list of available command line commands.
        /// </summary>
        private static readonly IEnumerable<string> availableCommands = new List<string>
        {
            "translate",
            "translate-document",
            "get-usage-statistics",
            "get-supported-languages"
        };

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Converts a language name to a language code.
        /// </summary>
        /// <param name="languageName">The name of the language that is to be converted.</param>
        /// <returns>
        /// Returns the converted language code. If the language name is not known, then it is assumed, that the language name is already a
        /// language code.
        /// </returns>
        private static string GetLanguageCode(string languageName)
        {
            // Defines a conversion map from language name to language code
            Dictionary<string, string> languageNameConversionMap = new Dictionary<string, string>
            {
                ["german"] = "DE",
                ["english"] = "EN",
                ["french"] = "FR",
                ["italian"] = "IT",
                ["japanese"] = "JA",
                ["spanish"] = "ES",
                ["dutch"] = "NL",
                ["polish"] = "PL",
                ["portuguese"] = "PT",
                ["brazilianportuguese"] = "PT-BR",
                ["brazilian-portuguese"] = "PT-BR",
                ["russian"] = "RU",
                ["chinese"] = "ZH"
            };

            // If the specified language is one of the known language names, then it is converted to its language code, otherwise it is
            // assumed that the specified language already is a language code
            if (languageNameConversionMap.ContainsKey(languageName.ToLowerInvariant()))
                return languageNameConversionMap[languageName.ToLowerInvariant()];
            return languageName;
        }

        /// <summary>
        /// Translates the specified text from the specified source language to the specified target language.
        /// </summary>
        /// <param name="authenticationKey">The authentication key for the DeepL API.</param>
        /// <param name="text">The text that is to be translated.</param>
        /// <param name="sourceLanguage">The language of the text.</param>
        /// <param name="targetLanguage">The language into which the text is to be converted.</param>
        private static async Task TranslateAsync(string authenticationKey, string text, string sourceLanguage, string targetLanguage)
        {
            using (DeepLClient client = new DeepLClient(authenticationKey))
            {
                Translation translation = await client.TranslateAsync(
                    text,
                    sourceLanguage == null ? null : Program.GetLanguageCode(sourceLanguage),
                    Program.GetLanguageCode(targetLanguage)
                );

                Console.WriteLine($"Detected source language: {translation.DetectedSourceLanguage}");
                Console.WriteLine();
                Console.WriteLine("Translation:");
                Console.WriteLine(translation.Text);
            }
        }

        /// <summary>
        /// Translates the specified document from the specified source language to the specified target language and saves the translated
        /// document into the specified file.
        /// </summary>
        /// <param name="authenticationKey">The authentication key for the DeepL API.</param>
        /// <param name="inputFile">The file that contains the document that is to be translated.</param>
        /// <param name="outputFile">The file into which the translated document is to be saved.</param>
        /// <param name="sourceLanguage">The language of the text.</param>
        /// <param name="targetLanguage">The language into which the text is to be converted.</param>
        private static async Task TranslateDocumentAsync(
            string authenticationKey,
            string inputFile,
            string outputFile,
            string sourceLanguage,
            string targetLanguage)
        {
            using (DeepLClient client = new DeepLClient(authenticationKey))
            {
                await client.TranslateDocumentAsync(
                    inputFile,
                    outputFile,
                    sourceLanguage == null ? null : Program.GetLanguageCode(sourceLanguage),
                    Program.GetLanguageCode(targetLanguage)
                );
            }
        }

        /// <summary>
        /// Gets the usage statistics and writes the to the console.
        /// </summary>
        /// <param name="authenticationKey">The authentication key for the DeepL API.</param>
        private static async Task GetUsageStatisticsAsync(string authenticationKey)
        {
            using (DeepLClient client = new DeepLClient(authenticationKey))
            {
                UsageStatistics usageStatistics = await client.GetUsageStatisticsAsync();
                Console.WriteLine($"Currently billed characters: {usageStatistics.CharacterCount}");
                Console.WriteLine($"Character limit:             {usageStatistics.CharacterLimit}");
            }
        }

        /// <summary>
        /// Gets the supported languages and writes them to the console.
        /// </summary>
        /// <param name="authenticationKey">The authentication key for the DeepL API.</param>
        private static async Task GetSupportedLanguagesAsync(string authenticationKey)
        {
            using (DeepLClient client = new DeepLClient(authenticationKey))
            {
                IEnumerable<SupportedLanguage> supportedLanguages = await client.GetSupportedLanguagesAsync();
                foreach (SupportedLanguage supportedLanguage in supportedLanguages)
                    Console.WriteLine($"{supportedLanguage.Name} ({supportedLanguage.LanguageCode})");
            }
        }

        /// <summary>
        /// Writes the help of the application to the console and quits the application.
        /// </summary>
        private static void WriteUsageAndExit()
        {
            // Writes the usage of the application to the console
            Console.WriteLine("DeepL command line tool.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("    deepl-cli translate [authentication-key] [text] [source-language] [target-language]");
            Console.WriteLine("    deepl-cli translate [authentication-key] [text] [target-language]");
            Console.WriteLine(
                "    deepl-cli translate-document [authentication-key] [input-file] [output-file] [source-language] [target-language]"
            );
            Console.WriteLine("    deepl-cli translate-document [authentication-key] [input-file] [output-file] [target-language]");
            Console.WriteLine("    deepl-cli get-usage-statistics [authentication-key]");
            Console.WriteLine("    deepl-cli get-supported-languages [authentication-key]");
            Console.WriteLine("    deepl-cli -h | --help");
            Console.WriteLine("    deepl-cli --version");

            // Exits the application with an error code
            Environment.Exit(1);
        }

        /// <summary>
        /// Writes the help page to the console and quits the application.
        /// </summary>
        /// <param name="command"></param>
        public static void WriteHelpAndExit(string command = null)
        {
            // When no command was specified, then the general help is printed to the console, otherwise the command specific help is
            // printed to the console
            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("DeepL command line tool.");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("    deepl-cli [command] [authentication-key] [command-arguments]");
                Console.WriteLine("    deepl-cli --help|-h");
                Console.WriteLine("    deepl-cli [command] --help|-h");
                Console.WriteLine("    deepl-cli --version");
                Console.WriteLine();
                Console.WriteLine("options:");
                Console.WriteLine("    --help|-h                 Show this help and exit.");
                Console.WriteLine("    --version                 Show the version.");
                Console.WriteLine();
                Console.WriteLine("command:");
                Console.WriteLine("    translate                 Translate text.");
                Console.WriteLine("    translate-document        Translate Word, PowerPoint, HTML, or text documents.");
                Console.WriteLine("    get-usage-statistics      Get the number of characters billed to your account and your limits.");
                Console.WriteLine("    get-supported-languages   List the languages currently supported by DeepL.");
                Console.WriteLine();
                Console.WriteLine("command options:");
                Console.WriteLine("    --help|-h                 Show detailed help for the specified command.");
                Console.WriteLine("    [authentication-key]      Your authentication key for the DeepL API, which you will find in the");
                Console.WriteLine("                              account settings https://www.deepl.com/pro-account.");
            }
            else
            {
                if (command == "translate")
                {
                    Console.WriteLine("Translate text.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("    deepl-cli translate [authentication-key] [text] [source-language] [target-language]");
                    Console.WriteLine("    deepl-cli translate [authentication-key] [text] [target-language]");
                    Console.WriteLine("    deepl-cli translate --help|-h");
                    Console.WriteLine();
                    Console.WriteLine("options:");
                    Console.WriteLine("    [authentication-key]      Your authentication key for the DeepL API, which you will find in");
                    Console.WriteLine("                              the account settings https://www.deepl.com/pro-account.");
                    Console.WriteLine("    [text]                    The text that is to be translated.");
                    Console.WriteLine("    [source-language]         The language of the text that is to be translated. If not specified,");
                    Console.WriteLine("                              the source language is inferred from the text, if possible.");
                    Console.WriteLine("    [target-language]         The language into which the text is to be translated.");
                    Console.WriteLine("    --help|-h                 Shows this help and exits.");
                }
                else if (command == "translate-document")
                {
                    Console.WriteLine("Translate Word, PowerPoint, HTML, or text documents.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine(
                        "    deepl-cli translate-document [authentication-key] [input-file] [output-file] [source-language] [target-language]"
                    );
                    Console.WriteLine("    deepl-cli translate-document [authentication-key] [input-file] [output-file] [target-language]");
                    Console.WriteLine("    deepl-cli translate --help|-h");
                    Console.WriteLine();
                    Console.WriteLine("options:");
                    Console.WriteLine("    [authentication-key]      Your authentication key for the DeepL API, which you will find in");
                    Console.WriteLine("                              the account settings https://www.deepl.com/pro-account.");
                    Console.WriteLine("    [input-file]              The path to the document is to be translated.");
                    Console.WriteLine("    [output-file]             The path to a file to which the translated document is to be saved.");
                    Console.WriteLine("    [source-language]         The language of the text that is to be translated. If not specified,");
                    Console.WriteLine("                              the source language is inferred from the text, if possible.");
                    Console.WriteLine("    [target-language]         The language into which the text is to be translated.");
                    Console.WriteLine("    --help|-h                 Shows this help and exits.");
                }
                else if (command == "get-usage-statistics")
                {
                    Console.WriteLine("Get the number of characters billed to your account and your limits.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("    deepl-cli get-usage-statistics [authentication-key]");
                    Console.WriteLine("    deepl-cli translate --help|-h");
                    Console.WriteLine();
                    Console.WriteLine("options:");
                    Console.WriteLine("    [authentication-key]      Your authentication key for the DeepL API, which you will find in");
                    Console.WriteLine("                              the account settings https://www.deepl.com/pro-account.");
                    Console.WriteLine("    --help|-h                 Shows this help and exits.");
                }
                else if (command == "get-supported-languages")
                {
                    Console.WriteLine("List the languages currently supported by DeepL.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("    deepl-cli get-supported-languages [authentication-key]");
                    Console.WriteLine("    deepl-cli translate --help|-h");
                    Console.WriteLine();
                    Console.WriteLine("options:");
                    Console.WriteLine("    [authentication-key]      Your authentication key for the DeepL API, which you will find in");
                    Console.WriteLine("                              the account settings https://www.deepl.com/pro-account.");
                    Console.WriteLine("    --help|-h                 Shows this help and exits.");
                }
            }

            // Exits the application with a success code
            Environment.Exit(0);
        }

        /// <summary>
        /// Writes the version of the application to the console and quits the application.
        /// </summary>
        public static void WriteVersionAndExit()
        {
            // Writes the version number to the console
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);

            // Exits the application with a success code
            Environment.Exit(0);
        }

        #endregion
    }
}
