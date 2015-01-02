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
        MCTSTreeNode root;

        public void play(Game game)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine("MCTSAI creating search tree");
            root = new MCTSTreeNode();

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

            //root.DumpChildMeans();

            int bestLoc = root.GetBestMove();
            int worstLoc = root.GetWorstMove();

            var bestChild = root.GetChild(bestLoc);
            var worstChild = root.GetChild(worstLoc);

            Console.WriteLine("MCTSAI spread from {0} ({1}) to {2} ({3})",
                bestChild.GetMean(), bestChild.GetNumEvals(),
                worstChild.GetMean(), worstChild.GetNumEvals());

            game.ApplyMove(bestLoc);
        }

        public List<int> getExpectedResponse()
        {
            MCTSTreeNode node = root;
            if (node == null) return new List<int>();
            int loc = node.GetBestMove();
            node = node.GetChild(loc);

            int player = node.GetPlayer();
            List<int> ret = new List<int>();
            while (node.GetPlayer() == player && node.AnyExpanded()) {
                loc = node.GetBestMove();
                node = node.GetChild(loc);

                if (loc != Game.locLen) {
                    ret.Add(loc);
                }
            }

            return ret;
        }
    }
}
