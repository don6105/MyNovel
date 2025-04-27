using System.Data;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace MyNovel
{
    public class Crawler
    {
        public void run(int? bookID = null) {

            NovelDB db = new();
            DataTable dt = db.dbGetBookUrl(bookID);

            string pattern = @"\/([^\/]+)\/$";
            string book_id; //目標網站的ID
            string book_url;
            string http_content;
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                book_url = dt.Rows[r]["url"].ToString();
                try
                {
                    Match m = Regex.Match(book_url, pattern);
                    book_id = m.Groups[1].Captures[0].ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("get <book_id> fail: " + ex.Message);
                    continue;
                }

                // 取得章節目錄
                http_content = Task.Run(() => getHttpContent(dt.Rows[r]["url"].ToString(), Int32.Parse(book_id))).Result;
                JObject json = JObject.Parse(http_content);
                //Console.WriteLine("共" + json["data"].Count() + "章");

                // 取得章節內容(平行)
                List<Task<Chapter>> task_list = new List<Task<Chapter>>();
                int jr = 0;
                foreach (var item in json["data"])
                {
                    task_list.Add(Task.Run(() => {
                        Chapter ch = new();
                        ch.my_book_id = int.Parse(dt.Rows[r]["bookID"].ToString());
                        ch.chapter_id = int.Parse(item["ordernum"].ToString());
                        ch.chapter_url = $"{book_url}p{item["ordernum"]}.html";
                        var pc = parseChapter(ch.chapter_url);
                        ch.title       = pc[0];
                        ch.content     = pc[1];
                        Console.WriteLine(ch.title);
                        return ch;
                    }));
                    jr++;
                    // 每批30個連線請求
                    if(jr%30 == 0)
                    {
                        Task.WaitAll(task_list.ToArray());
                    }
                }
                Task.WaitAll(task_list.ToArray());

                foreach (var task in task_list)
                {
                    var t = task.Result;
                    if (t.title.IndexOf("第") > -1 && t.title.IndexOf("章") > -1)
                    {
                        //Console.WriteLine(t.title);
                        db.dbAddChapter(t.my_book_id, t.title, t.content);
                    }
                }
                db.dbUpdateChapterCnt(int.Parse(dt.Rows[r]["bookID"].ToString()));
            }
        }

        private static async Task<string> getHttpContent(string url, int bookID)
        {
            string result = "";
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://ixdzs.tw/novel/clist/");
                var collection = new List<KeyValuePair<string, string>>();
                collection.Add(new("bid", bookID.ToString()));
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
                if (p1 != -1 && p2 != -1) {
                    content = content.Substring(p1, p2 - p1 + 1);
                }

                // 拆出「章節標題」及「內容」
                step = "[Step.2]";
                Match m = Regex.Match(content, @"<h3>.+<\/h3>");
                title = m.Value.Replace("<h3>", "").Replace("</h3>", "");
                content = content.Replace(m.Value, "");

                // 去掉<script>語法
                step = "[Step.3]";
                do {
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

    struct Chapter
    {
        public int my_book_id;
        public int chapter_id;
        public string chapter_url;
        public string? title;
        public string? content;
    }
}
