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

        public static async Task<bool> IsValidAndReachableUrlAsync(string url, HttpClient httpClient)
        {
            if(!IsValidUrl(url))
                return false;

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
