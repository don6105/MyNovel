namespace MyNovel
{
    public abstract class Parser
    {
        public abstract List<Chapter>? step1(string myBookID, string bookURL);
        public abstract Chapter step2(Chapter ch);
    }
}
