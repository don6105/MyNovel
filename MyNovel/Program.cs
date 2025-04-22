using System.Net;
using MyNovel;

//[RazorSlices] https://github.com/DamianEdwards/RazorSlices

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

NovelController novel_ctr = new();
//app.MapGet("/", () => "Welcome MyNovel!");
app.MapGet("/", () => { //首頁自動跳轉到書架
    return Results.Redirect("/bookshelf");
});
app.MapGet("/bookshelf/{page:int=1}", (int page) => {
    return Results.Content(novel_ctr.getBookList(page), "text/html");
});
app.MapGet("/book/{bookID:int}", (int bookID) => {
    return Results.Content(novel_ctr.getChapterList(bookID), "text/html");
});
app.MapGet("/book/{bookID:int}/{chapterID:int}", (int bookID, int chapterID) => {
    return Results.Content(novel_ctr.getArticle(bookID, chapterID), "text/html");
});

//cshtml -> build action -> must be content
app.MapGet("/addbook", () => Results.Extensions.RazorSlice<MyNovel.Slices.AddBook>());
app.MapPost("/addbook", (HttpRequest request) => {
    return Results.Content(novel_ctr.addBook(request), "text/html");
});

app.MapGet("/addchapter/{bookID:int}", (int bookID) => {
    NovelDB db = new();
    string param = bookID.ToString() + "#!#" + db.dbGetBookName(bookID);
    return Results.Extensions.RazorSlice<MyNovel.Slices.AddChapter, string>(param);
});
app.MapPost("/addchapter/{bookID:int}", (int bookID, HttpRequest request) =>
{
    return Results.Content(novel_ctr.addChapter(bookID, request), "text/html");
});

app.Run();
