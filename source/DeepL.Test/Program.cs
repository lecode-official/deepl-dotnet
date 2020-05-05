
#region Using Directives

using System;
using System.Threading.Tasks;

#endregion

namespace DeepL.Test
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
            // Checks if the API authentication key was passed as a command line argument
            if (arguments.Length != 1)
            {
                Console.WriteLine("The authentication key must be passed as the first argument to the application.");
                return;
            }

            // Retrieves the usage statistics from the DeepL API and prints them to the console
            using (DeepLClient client = new DeepLClient(arguments[0]))
            {
                try
                {
                    UsageStatistics usageStatistics = await client.GetUsageStatisticsAsync();
                    Console.WriteLine($"Character count: {usageStatistics.CharacterCount}");
                    Console.WriteLine($"Character limit: {usageStatistics.CharacterLimit}");
                }
                catch (DeepLException exception)
                {
                    Console.WriteLine($"An error occurred: {exception.Message}");
                }
            }
        }

        #endregion
    }
}
