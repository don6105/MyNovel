using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace MyNovel
{
    //愛下電子書
    public class IXDZS : Parser
    {
        const string catalogue_url = "https://ixdzs.tw/novel/clist/";

        public override List<Chapter>? step1(string myBookID, string bookURL)
        {
            string? target_book_id = getTargetBookID(bookURL);
            if (target_book_id == null) {
                return null;
            }

            string http_body;
            List<Chapter> chs = new();
            http_body = Task.Run(() => getCatalogue(target_book_id)).Result;
            JObject json = JObject.Parse(http_body);
            foreach (var item in json["data"])
            {
                Chapter ch = new();
                ch.my_book_id = int.Parse(myBookID);
                ch.chapter_id = int.Parse(item["ordernum"].ToString());
                ch.chapter_url = $"{bookURL}p{item["ordernum"]}.html";
                chs.Add(ch);
            }
            return chs;
        }

        public override Chapter step2(Chapter ch)
        {
            string[] content = parseChapter(ch.chapter_url);
            ch.title   = content[0];
            ch.content = content[1];
            return ch;
        }

        // 由小說網址解析對方的bookID
        private string? getTargetBookID(string bookURL)
        {
            try
            {
                string pattern = @"\/([^\/]+)\/$";
                Match m = Regex.Match(bookURL, pattern);
                return m.Groups[1].Captures[0].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("get <book_id> fail: " + ex.Message);
                return null;
            }
        }

        // 取得目錄頁
        private static async Task<string> getCatalogue(string bookID)
        {
            string result = "";
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, catalogue_url);
                var collection = new List<KeyValuePair<string, string>>();
                collection.Add(new("bid", bookID)); //POST資料
                var content = new FormUrlEncodedContent(collection);
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine($"Message: {e.Message} ");
            }
            return result;
        }

        // 取得小說內文
        private string[] parseChapter(string chapterUrl)
        {
            string[] chapter = new string[2];
            string step      = "";
            string title     = "";
            string content   = "";
            int p1, p2;
            try
            {
                var client = new HttpClient();
                var response = client.GetAsync(chapterUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();

                // 取得章節內容
                step = "[Step.1]";
                string pattern = @"\/([^\/]+)\/$";
                p1 = content.IndexOf("<article class=\"page-content\">");
                p2 = content.LastIndexOf("</article>") + "</article>".Length;
                if (p1 != -1 && p2 != -1)
                {
                    content = content.Substring(p1, p2 - p1 + 1);
                }

                // 拆出「章節標題」及「內容」
                step = "[Step.2]";
                Match m = Regex.Match(content, @"<h3>.+<\/h3>");
                title = m.Value.Replace("<h3>", "").Replace("</h3>", "");
                content = content.Replace(m.Value, "");

                // 去掉<script>語法
                step = "[Step.3]";
                do
                {
                    p1 = content.IndexOf("<script");
                    p2 = content.IndexOf("</script>") + "</script>".Length;
                    if (p1 != -1 && p2 != -1)
                    {
                        content = content.Remove(p1, p2 - p1 + 1);
                    }
                } while (p1 != -1 && p2 != -1);

                step = "[Step.4]";
                chapter[0] = title;
                chapter[1] = content;
                return chapter;
            }
            catch (Exception ex)
            {
                Console.WriteLine(step + " " + ex.Message);
                Console.WriteLine(chapterUrl);
                //Console.WriteLine(result);
                //System.Environment.Exit(1);
            }
            return chapter;
        }
    }
}
