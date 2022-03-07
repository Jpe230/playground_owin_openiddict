using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace Demo.Client
{
    class TokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string id_token { get; set; }
    }
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Press a key to continue...");
            Console.ReadLine();

            string tokenUrl = "https://localhost:44336/auth/token";
            string apiUrl = "https://localhost:44385/api/message";

            // Parameters eent to the Auth server
            Dictionary<string, string> tokenRequest = new Dictionary<string, string>();
            
            tokenRequest.Add("grant_type", "client_credentials");
            tokenRequest.Add("client_id", "postman-1");
            tokenRequest.Add("client_secret", "postman-secret");
            tokenRequest.Add("scope", "openid api1");

            FormUrlEncodedContent formTokenRequest = new FormUrlEncodedContent(tokenRequest);

            using HttpClient client = new();

            Console.WriteLine("Requesting a Token...");

            // Post to Auth server to retrieve the token
            HttpResponseMessage response = await client.PostAsync(tokenUrl, formTokenRequest);

            // Read the reult and deserialize it
            string result = response.Content.ReadAsStringAsync().Result;
            TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(result);
            Console.WriteLine(result);

            // Add token to Authorization header to call API
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.access_token);

            Console.WriteLine("Calling API...");
            // Call API, result should be 200 if token is correct and present. 401 is auth failed
            response = await client.GetAsync(apiUrl);
            Console.WriteLine(response);

            Console.WriteLine("Press a key to exit...");
            Console.ReadLine();


        }
    }
}
