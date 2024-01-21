using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GitDrive
{
    internal class GitHubApi
    {
        private static SHA1 hashAlg = SHA1.Create();

        private static HttpClient client = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All,
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true,
        })
        {
            BaseAddress = new Uri("https://api.github.com/"),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            DefaultRequestHeaders = { 
                {"User-Agent", "Awesome-Octocat-App" }
            }
        };

        private static JsonSerializerOptions options = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static string GitUsername { get; set; }

        public static string GitToken { get; set; }

        public static string GitRepoName { get; set; }


        public static string DefaultBranch;

        public static string ComitSha;
        public static string TreeSha;

        public static async Task Init()
        {
            JsonNode json = JsonNode.Parse(await SendRequest(new HttpRequestMessage(HttpMethod.Get, "repos/" + GitUsername + "/" + GitRepoName)));

            DefaultBranch = (string)json["default_branch"];

            Console.WriteLine(json);

            await GetBranch();
        }
        public static async Task GetBranch() => await GetBranch(DefaultBranch);
        public static async Task GetBranch(string branch)
        {
            JsonNode json = JsonNode.Parse(await SendRequest(new HttpRequestMessage(HttpMethod.Get, "repos/" + GitUsername + "/" + GitRepoName + "/branches/" + branch)));
           
            Console.WriteLine(json);

            ComitSha = (string)json["commit"]["sha"];
            TreeSha = (string)json["commit"]["commit"]["tree"]["sha"];

            return;
        }

        /// <summary>
        /// Returns hash of the stree
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static async Task<string> CreateTree(Tree tree)
        {
            string jsonstr = JsonSerializer.Serialize(tree, options);

            var req = new HttpRequestMessage(HttpMethod.Post, "repos/" + GitUsername + "/" + GitRepoName + "/git/trees")
            {
                Content = new StringContent(jsonstr)
            };

            JsonNode json = JsonNode.Parse(await SendRequest(req));

            Console.WriteLine(json);


            return (string)json["sha"];
        }

        public static async void UploadFile(Commit commit)
        {
            var fileData = File.ReadAllBytes(commit.Message);

            var req = new HttpRequestMessage(HttpMethod.Put, "repos/" + GitUsername + "/" + GitRepoName + "/contents/hello.txt")
            {
                Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "message", "txt file" },
                    { "content", Convert.ToBase64String(fileData) },
                    { "sha", BitConverter.ToString(hashAlg.ComputeHash(fileData)).Replace("-", "").ToLower() }
                }, options))
            };

            Console.WriteLine(await SendRequest(req));
        }
        public static async void GetFile(string path)
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, "repos/" + GitUsername + "/" + GitRepoName + "/contents/hello.txt"))
            {
                req.Version = HttpVersion.Version30;
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GitToken);
            

                using (var res = await client.SendAsync(req))
                {
                    Console.WriteLine(await res.Content.ReadAsStringAsync());
                }
            }
        }
        public static async Task GetRateLimit()
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, "rate_limit"))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GitToken);
                
                using(var res = await client.SendAsync(req))
                {
                    Console.WriteLine(await res.Content.ReadAsStringAsync());
                }
            }
        }

        private static async Task<string> SendRequest(HttpRequestMessage request)
        {
            using (var req = request)
            {
                req.Version = HttpVersion.Version30;
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GitToken);

                using (var res = await client.SendAsync(req))
                {
                    return  await res.Content.ReadAsStringAsync();
                }
            }
        }
    }
}
