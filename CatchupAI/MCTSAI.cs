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
        public class NodeLink
        {
            public MCTSTreeNode node;
            public NodeLink next;

            public NodeLink(MCTSTreeNode node, NodeLink next) {
                this.node = node;
                this.next = next;
            }
        }

        public static Random rng = new Random();

        long TIME_MS = 2000;
        MCTSTreeNode root;

        // For the RAVE heuristic:
        // player -> move -> node (sibling of current path)
        public NodeLink[][] raveTable;

        public void play(Game game)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine("MCTSAI creating search tree");
            root = new MCTSTreeNode(this);

            Game gameCopy = new Game();

            int numIterations = 0;
            while (watch.ElapsedMilliseconds < TIME_MS)
            {
                ++numIterations;
                game.CopyTo(gameCopy);

                // Clear RAVE table because it only updates siblings.
                raveTable = new NodeLink[2][];
                for (int player = 0; player < 2; ++player)
                {
                    raveTable[player] = new NodeLink[Game.locLen + 1];
                }

                root.select(gameCopy);
            }
            Console.WriteLine("MCTSAI ran {0} iterations in {1} ms", numIterations,
                watch.ElapsedMilliseconds);

            //root.DumpChildMeans();

            int bestLoc = root.GetBestMove();
            int robustLoc = root.GetRobustMove();
            int worstLoc = root.GetWorstMove();

            var bestChild = root.GetChild(bestLoc);
            var robustChild = root.GetChild(robustLoc);
            var worstChild = root.GetChild(worstLoc);

            Console.WriteLine("MCTSAI root moves:");
            Console.Write("Best:    ");
            bestChild.DumpStats();
            Console.Write("Robust:  ");
            robustChild.DumpStats();
            Console.Write("Worst:   ");
            worstChild.DumpStats();

            game.ApplyMove(bestLoc);
        }

        public void putNodeInRaveTable(MCTSTreeNode node, int player, int move)
        {
            raveTable[player][move] = new NodeLink(node, raveTable[player][move]);
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
