﻿using System.Reflection;
using System.Text.Json;

namespace AWGBassumTelegramBot.Misc
{
    public static class Helper
    {
        public static bool IsValidUrl(string url)
        {
            if(string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
                   && !string.IsNullOrEmpty(result.Host)
                   && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        public static async Task<bool> IsValidAndReachableUrlAsync(HttpClient httpClient)
        {
            AppSettings? settings = Helper.ReadSettings<AppSettings>();

            if(settings is null)
                return default;

            if(!IsValidUrl(settings.CalendarUrl))
                return false;

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, settings.CalendarUrl));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        internal static T? ReadSettings<T>()
        {
            // Determine the file path based on whether the code is running inside a container
            string path;
            if(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                // In a container environment, use the app-specific path
                path = @"/app/appdata/settings.json";

                // Ensure the directory exists before attempting to read or write the settings file
                string? directory = Path.GetDirectoryName(path);
                if(!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            else
            {
                // In a non-container environment (e.g., local development), use a relative file path
                path = "appdata/settings.json";
            }

            // Open the file for reading; the `using` statement ensures the file is closed/disposed automatically
            using StreamReader r = new(path);
            // Read the entire JSON content from the file
            string json = r.ReadToEnd();

            // Deserialize the JSON into the concrete SettingsModel type
            Settings? settings = JsonSerializer.Deserialize<Settings>(json);

            // If deserialization failed (e.g., file is empty or JSON is invalid), return the default value for T
            if(settings is null)
            {
                return default;
            }

            // Extract and return the requested setting of type T from the SettingsModel
            return settings.GetSetting<T>();
        }

        /// <summary>
        /// Extension method on <see cref="SettingsModel"/> that retrieves the first public instance property
        /// whose type exactly matches <typeparamref name="T"/> and returns its value.
        /// </summary>
        /// <typeparam name="T">The type of the setting to retrieve.</typeparam>
        /// <param name="settings">The SettingsModel instance to search.</param>
        /// <returns>
        /// The value of the first matching property cast to <typeparamref name="T"/>,
        /// or <c>null</c> if <paramref name="settings"/> is <c>null</c> or no matching property is found.
        /// </returns>
        internal static T? GetSetting<T>(this Settings settings)
        {
            // Guard: if settings is null, the null-conditional operator here will short-circuit and return null
            return (T?)settings?
                // Get all public instance properties defined on the SettingsModel type
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                // Find the first property whose declared type exactly equals T
                .First(prop => prop.PropertyType == typeof(T))
                // Read the value of that property from the settings object
                .GetValue(settings, null);
        }
    }
}
