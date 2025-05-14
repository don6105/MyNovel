using System.Data;
using MySqlConnector;

namespace MyNovel
{
    //參考 https://ithelp.ithome.com.tw/articles/10332732
    public class NovelDB
    {
        private string database;
        private string server;
        private string account;
        private string password;
        private MySqlConnection conn;

        public NovelDB()
        {
            // Build a config object, using env vars and JSON providers.
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("DB.json")
                .AddEnvironmentVariables()
                .Build();
            
            database = config.GetValue<string>("database") ?? "my_novel";
            server   = config.GetValue<string>("server") ?? "localhost";
            account  = config.GetValue<string>("account") ?? "root";
            password = config.GetValue<string>("password") ?? "xxx";            

            string connStr = $"server={server};" + $"user={account};" +
                             $"password={password};" + $"database={database};" +
                             "charset=utf8;" +
                             "AllowLoadLocalInfile=true;"; //大量塞資料用
            conn = new MySqlConnection(connStr);
            try
            {
                conn.Open(); //資料庫連線
                Console.WriteLine("DB connect succ");
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 4060:
                        Console.WriteLine("Invalid Database");
                        break;
                    case 18456:
                        Console.WriteLine("Login Failed");
                        break;
                    default:
                        Console.WriteLine("MySQL Connect Failed");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine(": " + ex.InnerException.Message);
                        }
                        break;
                }
            }
        }

        ~NovelDB()
        {
            try
            {
                conn.Close(); //資料庫斷線
                Console.WriteLine(conn.State);
            }
            catch { }
        }

        public DataTable dbGetBook(int page = 1)
        {
            int offset = page > 0 ? (page - 1) * 50 : 0;
            string sql = "SELECT bookID, name as bookName, author, chapterCnt, url, isEnd, updateTime " +
                         $"FROM book ORDER BY bookID LIMIT 50 OFFSET {offset}";

            using (MySqlDataAdapter da = new MySqlDataAdapter(sql, conn))
            {
                MySqlCommandBuilder bd = new MySqlCommandBuilder(da);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public string dbGetBookName(int bookID)
        {
            string sql = $"SELECT name as bookName FROM book WHERE bookID={bookID} LIMIT 1";

            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string book_name = reader.GetString("bookName");
                        return book_name;
                    }
                }
            }
            return "";
        }

        public DataTable dbGetBookUrl(int? bookID = null)
        {
            string sql = "";
            sql  = "SELECT name, bookID, url FROM book ";
            sql += (bookID == null) ? "WHERE 1=1 " : $"WHERE bookID={bookID} ";
            sql += "ORDER BY bookID";

            using (MySqlDataAdapter da = new MySqlDataAdapter(sql, conn))
            {
                MySqlCommandBuilder bd = new MySqlCommandBuilder(da);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable dbGetChapter(int bookID)
        {
            string sql = $"SELECT chapterID, title FROM chapter WHERE bookID={bookID}";

            using (MySqlDataAdapter da = new MySqlDataAdapter(sql, conn))
            {
                MySqlCommandBuilder bd = new MySqlCommandBuilder(da);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable dbGetArticle(int bookID, int chapterID)
        {
            string sql = $"SELECT title, content FROM chapter WHERE bookID={bookID} AND chapterID={chapterID}";

            using (MySqlDataAdapter da = new MySqlDataAdapter(sql, conn))
            {
                MySqlCommandBuilder bd = new MySqlCommandBuilder(da);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public int dbAddBook(string bookName, string author, string url, bool isEnd)
        {
            int row_cnt = 0;
            string sql = "INSERT INTO book(name, author, url, isEnd) VALUES (@name, @author, @url, @isEnd)";
            using (MySqlCommand mc = new MySqlCommand(sql, conn))
            {
                mc.Parameters.AddWithValue("@name", bookName);
                mc.Parameters.AddWithValue("@author", author);
                mc.Parameters.AddWithValue("@url", url);
                mc.Parameters.AddWithValue("@isEnd", isEnd);

                row_cnt = mc.ExecuteNonQuery();
            }
            return row_cnt;
        }

        public int dbClearChapters(int bookID)
        {
            int row_cnt = 0;
            string sql = "DELETE FROM chapter WHERE bookID=@bookID";
            using (MySqlCommand mc = new MySqlCommand(sql, conn))
            {
                mc.Parameters.AddWithValue("@bookID", bookID);
                row_cnt = mc.ExecuteNonQuery();
            }
            return row_cnt;
        }
        public int dbAddChapter(int bookID, string title, string content)
        {
            int row_cnt = 0;
            string chapter_id = "";
            string sql;

            sql = $"SELECT ifnull(max(chapterID),0)+1 as chapterID FROM chapter WHERE bookID={bookID} LIMIT 1";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chapter_id = reader.GetInt64("chapterID").ToString();
                    }
                }
            }

            sql = "INSERT INTO chapter(bookID, chapterID, title, content) VALUES (@bookID, @chapterID, @title, @content)";
            using (MySqlCommand mc = new MySqlCommand(sql, conn))
            {
                mc.Parameters.AddWithValue("@bookID", bookID);
                mc.Parameters.AddWithValue("@chapterID", chapter_id);
                mc.Parameters.AddWithValue("@title", title);
                mc.Parameters.AddWithValue("@content", content);

                row_cnt = mc.ExecuteNonQuery();
            }

            return row_cnt;
        }

        public int dbAddChapterBulk(int bookID, Chapter[] chs)
        {
            int max_chapter_id = 0;
            string sql;
            //取得最大ID
            sql = $"SELECT ifnull(max(chapterID),0) as chapterID FROM chapter WHERE bookID={bookID} LIMIT 1";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        max_chapter_id = reader.GetInt32("chapterID");
                    }
                }
            }
            //轉成DataTable格式
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("bookID"));
            dt.Columns.Add(new DataColumn("chapterID"));
            dt.Columns.Add(new DataColumn("title"));
            dt.Columns.Add(new DataColumn("content"));
            foreach(var ch in chs) {
                DataRow r = dt.NewRow();
                r[0] = bookID;
                r[1] = ch.chapter_id;
                r[2] = ch.title;
                r[3] = ch.content;
                dt.Rows.Add(r);
            };
            //整批寫入MySQL
            List<MySqlBulkCopyColumnMapping> col_mappings = new List<MySqlBulkCopyColumnMapping>();
            col_mappings.Add(new MySqlBulkCopyColumnMapping(0, "bookID"));
            col_mappings.Add(new MySqlBulkCopyColumnMapping(1, "chapterID"));
            col_mappings.Add(new MySqlBulkCopyColumnMapping(2, "title"));
            col_mappings.Add(new MySqlBulkCopyColumnMapping(3, "content"));
            MySqlBulkCopy bulk_copy = new MySqlBulkCopy(conn);// 建立MySqlBulkCopy
            bulk_copy.DestinationTableName = "chapter"; // 目標Table名稱
            bulk_copy.ColumnMappings.AddRange(col_mappings);
            MySqlBulkCopyResult result = bulk_copy.WriteToServer(dt); // dataTable複製到資料庫

            Console.WriteLine($"上傳 {result.RowsInserted} 章");

            return result.RowsInserted;
        }

        public int dbUpdateChapterCnt(int bookID)
        {
            int row_cnt = 0;
            string sql = "UPDATE book A SET ChapterCnt=(SELECT count(*) FROM chapter WHERE bookID=A.bookID)" +
                         " WHERE bookID=@bookID";
            using (MySqlCommand mc = new MySqlCommand(sql, conn))
            {
                mc.Parameters.AddWithValue("@bookID", bookID);
                row_cnt = mc.ExecuteNonQuery();
            }

            return row_cnt;
        }

    } // class end
}
