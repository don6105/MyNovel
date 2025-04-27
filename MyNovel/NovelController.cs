using System.Data;

namespace MyNovel
{
    public class NovelController
    {
        const string boostrap_css = @"<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.5/dist/css/bootstrap.min.css' 
                                       rel='stylesheet' 
                                       integrity='sha384-SgOJa3DmI69IUzQ2PVdRZhwQ+dy64/BUtbMJw1MZ8t5HZApcHrRKUc4W0kG879m7'
                                       crossorigin='anonymous'>";
        const string bootstrap_js = @"<script src='https://cdn.jsdelivr.net/npm/bootstrap@5.3.5/dist/js/bootstrap.bundle.min.js'
                                       integrity='sha384-k6d4wzSIapyDyv1kpU366/PK5hCdSbCRGRCMv+eplOQJWyd1fbcAu9OCUj5zNLiq'
                                       crossorigin='anonymous'></script>";

        public string getBookList(int page = 1)
        {
            NovelDB db = new();
            DataTable dt = db.dbGetBook(page);
            string web_content = $"{boostrap_css} {bootstrap_js}";
            web_content += @"<style>
                               body { padding: 20px 20px; }
                               table, th, td {
                                 border-collapse: collapse;
                               } 
                               th, td {
                                 padding: 4px 8px;
                                 text-align: center;
                               }
                             </style>
                             <a class='btn btn-md btn-primary float-end mb-2' href='../addbook' role='button'>新增小說</a>
                             <table class='table table-bordered'>
                               <thead>
                                 <th> 項次 </th>
                                 <th style='min-width: 240px;'> 小說名稱 </th>
                                 <th> 章節數量 </th>
                                 <th> 作者 </th>
                                 <th> 狀態 </th>
                                 <th> 更新時間 </th>
                               </thead> 
                               <tbody> ";
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string tr = "";
                string state = Convert.ToBoolean(dt.Rows[r]["isEnd"])? "已完結" : "連載中";
                tr = $"<td> {r+1} </td>" +
                     $"<td> <a href='../book/{dt.Rows[r]["bookID"]}' target='_blank'>{dt.Rows[r]["bookName"]}</a> </td>" +
                     $"<td> {dt.Rows[r]["chapterCnt"]} </td>" +
                     $"<td> {dt.Rows[r]["author"]} </td>" +
                     $"<td> {state} </td>" +
                     $"<td> {dt.Rows[r]["updateTime"]} </td>";
                web_content += $"<tr> {tr} </tr>";
            }
            web_content += " </tbody> </table>";
            return web_content;
        }

        public string getChapterList(int bookID)
        {
            NovelDB db = new();
            DataTable dt = db.dbGetChapter(bookID);
            string web_content = $"{boostrap_css} {bootstrap_js}";
            web_content += @"<style> 
                               body { padding: 20px 20px; }
                               table, th, td { border-collapse: collapse; } 
                               td {
                                 padding: 4px 8px; 
                                 min-width: 200px;
                               }
                             </style> ";
            //標題
            web_content += $"<div style='text-align: center;'> <h2>" + db.dbGetBookName(bookID) + "</h2>";
            web_content += $"<a class='btn btn-md btn-primary float-end mb-2' href='../addchapter/{bookID}' role='button'>新增章節內容</a>";
            web_content += "<table class='table table-bordered'> <tbody> ";
            int col_limit = 5; //每5章換行
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                string tr = (r % col_limit == 0) ? "<tr>" : ""; 
                tr  = $"<td> <a href='../../book/{bookID}/{dt.Rows[r]["chapterID"]}' target='_blank'>{dt.Rows[r]["title"]}</a> </td>";
                tr += (r % col_limit == col_limit-1) ? "</tr>" : "";
                web_content += tr;
            }
            web_content += " </tbody> </table> </div>";
            return web_content;
        }

        public string getArticle(int bookID, int chapterID)
        {
            NovelDB db = new();
            DataTable dt = db.dbGetArticle(bookID, chapterID);
            string web_content = "<style> body { padding: 15px 3.5%; } </style>";
            web_content += $"{boostrap_css} {bootstrap_js}";
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                web_content += $"<h2> {dt.Rows[r]["title"]} </h2>";
                web_content += $"<h3> {dt.Rows[r]["content"].ToString().Replace("  ", "<br><br>")} </h3>";
            }
            return web_content;
        }

        public string addBook(HttpRequest request)
        {
            NovelDB db = new();
            string? book_name = request.Form["bookName"];
            string? author    = request.Form["author"];
            string? url       = request.Form["url"];
            bool    is_end    = string.IsNullOrWhiteSpace(request.Form["isEnd"])? false : true;

            int row_cnt = db.dbAddBook(book_name, author, url, is_end);
            return $"Inserted {row_cnt} book(s).";
        }

        public string addChapter(int bookID, HttpRequest request)
        {
            NovelDB db = new();
            string? title   = request.Form["title"];
            string? content = request.Form["content"];

            int row_cnt = db.dbAddChapter(bookID, title, content);
            return $"Inserted {row_cnt} chapter(s).";
        }

    } // class end
}
