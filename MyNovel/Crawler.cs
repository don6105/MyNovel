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

            string book_id; //目標網站的ID
            string book_url;
            string book_name;
            List<Chapter> chs;
            //string http_content;
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                book_url  = dt.Rows[r]["url"].ToString();
                book_id   = dt.Rows[r]["bookID"].ToString();
                book_name = dt.Rows[r]["name"].ToString();

                Downloader dl = new(new IXDZS());
                EventSubscriber subscriber = new EventSubscriber();
                Console.WriteLine($"[{book_name}]"); //印出小說名稱
                subscriber.Subscribe(dl); //訂閱事件:顯示進度條
                chs = dl.run(book_id, book_url, 20);

                db.dbClearChapters(int.Parse(book_id));
                db.dbAddChapterBulk(int.Parse(book_id), chs.ToArray()); //@@@ Todo:有特殊字元無法整批寫入
                db.dbUpdateChapterCnt(int.Parse(book_id));
                
                Console.WriteLine("\n");
            }
        }
    }
}
