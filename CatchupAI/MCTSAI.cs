using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatchupAI
{
    class MCTSAI
        : IPlayer
    {
        public static Random rng = new Random();

        long TIME_MS = 2000;

        public void play(Game game)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine("MCTSAI creating search tree");
            MCTSTreeNode root = new MCTSTreeNode();

            Game gameCopy = new Game();

            int numIterations = 0;
            while (watch.ElapsedMilliseconds < TIME_MS)
            {
                ++numIterations;
                game.CopyTo(gameCopy);
                root.select(gameCopy);
            }
            Console.WriteLine("MCTSAI ran {0} iterations in {1} ms", numIterations,
                watch.ElapsedMilliseconds);

            int loc = root.GetBestMove();
            game.ApplyMove(loc);
        }
    }
}
