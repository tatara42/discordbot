using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace bot
{
    public class GelbooruService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static string TagList = "";
        private static string Blacklist = "";
        private static int ImageCount = 1;
        private static string Artists = "";
        private static int Limit = 100;
        private static string Random = "";
        private static string Rating = "+rating:general";
        private static string? URL;

        private GelbooruService()
        {
            URL = "";
        }

        public static GelbooruService getURL()
        {
            return new GelbooruService();
        }
        public GelbooruService withTag(params string[] tags)
        {
            TagList = string.Join("%20", tags.Select(t => Regex.Replace(t, @"\s+", " ")));
            return this;
        }
        public GelbooruService withArtist(List<string> artist)
        {
            Artists = "+{" + string.Join(" ~ ", artist) + "}";
            return this;
        }

        public GelbooruService withNsfw(bool isNsfw)
        {
            if (isNsfw) Rating = "+-rating:general+-rating:sensitive";
            return this;
        }

        public GelbooruService withBlacklist(List<string> blacklist)
        {
            Blacklist = "+%20-" + string.Join("%20-", blacklist);
            return this;
        }

        public GelbooruService withImageCount(int count)
        {
            ImageCount = count;
            return this;
        }

        public GelbooruService setLimit(int limit)
        {
            Limit = limit;
            return this;
        }
        public GelbooruService withRandom(bool isRandom)
        {
            if (isRandom) Random = "sort:random";
            return this;
        }
        public async Task<string> Build()
        {
            try
            {
                var request = CreateHttpRequestAsync();

                var response = _httpClient.Send(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var imageUrl = ParseImageUrl(result);

                if (imageUrl == null)
                {
                    return "No Result!";
                }
                else
                {
                    return imageUrl;
                }
            }
            catch (HttpRequestException ex)
            {
                await Console.Out.WriteLineAsync(ex.ToString());
                return "Server Error!";
            }
        }
        public static HttpRequestMessage CreateHttpRequestAsync()
        {
            URL = $"https://gelbooru.com/index.php?page=dapi&s=post&q=index&tags={TagList}{Blacklist}{Rating}{Artists}{Random}&limit={Limit}&json=1";

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(URL),
                Method = HttpMethod.Get,
                Headers = { { "Accept", "*/*" } }
            };

            return request;
        }

        private static string ParseImageUrl(string result)
        {
            Random random = new Random();
            var json = JsonConvert.DeserializeObject<Root>(result);

            int count = json?.post?.Count ?? 0;

            if (count == 0)
            {
                return null;
            }

            string ImageURL = null;
            do
            {
                var rand = random.Next(0, count);

                if (json?.post?[rand].file_url == null)
                {
                    break;
                }
                else
                {
                    ImageURL += json.post[rand].file_url + "\n";
                }
                count--;
                ImageCount--;
            } while (count > 0 && ImageCount > 0);
            return ImageURL;
        }

    }
    public class Attributes
    {
        public int limit { get; set; }
        public int offset { get; set; }
        public int count { get; set; }
    }

    public class Post
    {
        public int id { get; set; }
        public string? created_at { get; set; }
        public int score { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string? md5 { get; set; }
        public string? directory { get; set; }
        public string? image { get; set; }
        public string? rating { get; set; }
        public string? source { get; set; }
        public int change { get; set; }
        public string? owner { get; set; }
        public int creator_id { get; set; }
        public int parent_id { get; set; }
        public int sample { get; set; }
        public int preview_height { get; set; }
        public int preview_width { get; set; }
        public string? tags { get; set; }
        public string? title { get; set; }
        public string? has_notes { get; set; }
        public string? has_comments { get; set; }
        public string? file_url { get; set; }
        public string? preview_url { get; set; }
        public string? sample_url { get; set; }
        public int sample_height { get; set; }
        public int sample_width { get; set; }
        public string? status { get; set; }
        public int post_locked { get; set; }
        public string? has_children { get; set; }
    }

    public class Root
    {
        [JsonProperty("@attributes")]
        public Attributes? attributes { get; set; }
        public List<Post>? post { get; set; }
    }

    public class ObjectJson
    {
        public List<string>? Artists { get; set; }
        public List<string>? Blacklist { get; set; }
    }
}
