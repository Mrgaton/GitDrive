using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GitDrive.Github
{
    internal class GitHubApi
    {//Ayudasaa https://www.youtube.com/watch?v=nwHqXtk6LHA https://www.youtube.com/watch?v=nwHqXtk6LHA https://www.youtube.com/watch?v=nwHqXtk6LHA
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
            var jsonNode = JsonNode.Parse(JsonSerializer.Serialize(tree, options)).AsObject();

            foreach (var el in jsonNode["tree"] as JsonArray)
            {
                if (el["content"] == null) jsonNode["tree"][el.GetElementIndex()].AsObject().Remove("content");
                if (el["content"] != null && el?["sha"] == null) jsonNode["tree"][el.GetElementIndex()].AsObject().Remove("sha");
            }

            var jsonstr = jsonNode.ToString();

            var req = new HttpRequestMessage(HttpMethod.Post, "repos/" + GitUsername + "/" + GitRepoName + "/git/trees")
            {
                Content = new StringContent(jsonstr)
            };

            JsonNode json = JsonNode.Parse(await SendRequest(req));

            Console.WriteLine(json);

            return (string)json["sha"];
        }

        public static async Task<RemoteTree> GetTree() => await GetTree(DefaultBranch);

        public static async Task<RemoteTree> GetTree(string branchName)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "repos/" + GitUsername + "/" + GitRepoName + "/git/trees/" + branchName + "?recursive=1");

            string json = await SendRequest(req);

            Console.WriteLine(json);

            return JsonSerializer.Deserialize<RemoteTree>(json);
        }

        public static async Task<string> CreateCommit(Commit commit)
        {
            string jsonstr = JsonSerializer.Serialize(commit, options);

            var req = new HttpRequestMessage(HttpMethod.Post, "repos/" + GitUsername + "/" + GitRepoName + "/git/commits")
            {
                Content = new StringContent(jsonstr)
            };

            JsonNode json = JsonNode.Parse(await SendRequest(req));

            Console.WriteLine(json);

            return (string)json["sha"];
        }

        public static async Task<string> CreateReference(Reference reference)
        {
            string jsonstr = JsonSerializer.Serialize(reference, options);

            var req = new HttpRequestMessage(HttpMethod.Patch, "repos/" + GitUsername + "/" + GitRepoName + "/git/refs/heads/" + DefaultBranch)
            {
                Content = new StringContent(jsonstr)
            };

            JsonNode json = JsonNode.Parse(await SendRequest(req));

            Console.WriteLine(json);

            return (string)json["object"]["sha"];
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

        /// <summary>
        /// Get current token rate limit
        /// </summary>
        /// <returns></returns>
        public static async Task GetRateLimit()
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, "rate_limit"))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GitToken);

                using (var res = await client.SendAsync(req))
                {
                    Console.WriteLine(await res.Content.ReadAsStringAsync());
                }
            }
        }

        public static async Task<byte[]> GetFileRaw(string path) => await GetFileRaw(GitUsername, GitRepoName, DefaultBranch, path);

        public static async Task<byte[]> GetFileRaw(string username, string repo, string branch, string path)
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, $"https://raw.githubusercontent.com/{username}/{repo}/{branch}/{path}"))
            {
                using (var res = await client.SendAsync(req))
                {
                    return await res.Content.ReadAsByteArrayAsync();
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
                    return await res.Content.ReadAsStringAsync();
                }
            }
        }
    }
}