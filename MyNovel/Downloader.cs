namespace MyNovel
{
    public class Downloader
    {
        public delegate void NotifyEventHandler(int cnt, int total);
        public event NotifyEventHandler NotifyEvent;

        private List<Chapter> chs;
        private Task[] ts;
        private Parser p;

        public Downloader(Parser p)
        {
            this.p = p; 
        }

        public List<Chapter> run(string myBookID, string bookURL, int taskNum = 20)
        {
            chs = p.step1(myBookID, bookURL);
            if (chs == null)
            {
                Console.WriteLine($"{myBookID} ({bookURL}) runs failed.");
                return chs;
            }

            ts = new Task[taskNum];
            int cnt = 0;
            do {
                cnt = setTask();
                Task.WaitAny(ts);
            } while (cnt > 0);
            Task.WaitAll(ts);

            return chs;
        }

        private int setTask()
        {
            int cnt = 0;
            for (int i = 0; i < ts.Length ; i++)
            {
                if (ts[i] == null || ts[i].IsCompleted) //找到未初始化或閒置的任務
                {
                    int idx = getTodoChapter();
                    if (idx > -1)
                    {
                        ts[i] = Task.Run(() => {
                            chs[idx] = p.step2(chs[idx]);
                            chs[idx].done = true;
                            //Console.WriteLine(chs[idx].title);
                            onNotifyEvent(getFinishedCount(), chs.Count()); //每一章完成後觸發訂閱通知
                        });
                        chs[idx].assigned = true;
                        cnt++;
                    }
                }
            }
            return cnt;
        }

        private int getTodoChapter()
        {
            if (chs == null) return -1;

            for (int i = 0; i < chs.Count; i++)
            {
                if (chs[i] == null) continue;
                if (chs[i].assigned == false && chs[i].done == false)
                {
                    return i;
                }
            }
            return -1;
        }

        private int getFinishedCount()
        {
            return chs.Where(ch => ch != null && ch.done).Count();
        }

        protected void onNotifyEvent(int cnt, int total)
        {
            NotifyEvent?.Invoke(cnt, total); //如果有訂閱者，就觸發訂閱事件
        }
    }

    //訂閱者
    public class EventSubscriber
    {
        public void Subscribe(Downloader dl)
        {
            dl.NotifyEvent += showProgressTip;
        }

        private void showProgressTip(int cnt, int total)
        {
            double percent = (total > 0)? Math.Round(Convert.ToDouble(cnt*100)/total) : 0;
            Console.Write($"\r進度：{cnt} / {total} ({percent}%) "); //顯示進度
        }
    }
}

        
