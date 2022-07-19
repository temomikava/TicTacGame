using System.Timers;

namespace GameLibrary.Helpers
{
    public class WaitingForOponent
    {
        public WaitingForOponent(int gameId, Func<int, Match> getActiveMatch)
        {
            GameId = gameId;
            GetActiveMatch = getActiveMatch;

        }
        public int GameId;
        public bool IsNotTerminated = true;
        public int Count;
        public object LockObj = new object();
        public Func<int, Match> GetActiveMatch;
        public Match Match;
        public Match Waiting()
        {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 5000;
            aTimer.Enabled = true;
            aTimer.Start();

            while (IsNotTerminated)
            {

            }
            

            
            
            aTimer.Stop();
            aTimer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);

            return Match;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            lock (LockObj)
            {
                var match =GetActiveMatch(GameId);

                if (match != null)
                {
                    //rija bazari
                    IsNotTerminated = false;
                    Match = match;

                }
            }
        }
    }
}
