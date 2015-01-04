using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CatchupAI
{
    class MCTSTreeNode
    {
        int numEvals = 0;
        int totalOutcome = 0;

        int player;
        MCTSTreeNode[] children;
        List<int> unexpandedMoves;
        List<int> expandedMoves;

        // Returns the outcome.
        // 1: Black win
        // 0: White win
        public int select(Game game)
        {
            if (children == null)
            {
                // Initializing is delayed until first selection.
                player = game.getCurrentPlayer();

                Debug.Assert(children == null);
                children = new MCTSTreeNode[Game.locLen + 1]; // locLen means pass

                Debug.Assert(unexpandedMoves == null);
                unexpandedMoves = game.getLegalMoves(true);

                Debug.Assert(expandedMoves == null);
                expandedMoves = new List<int>();
            }

            int outcome;
            if (unexpandedMoves.Count > 0)
            {
                int move = popRandomMove(unexpandedMoves);
                expandedMoves.Add(move);
                children[move] = new MCTSTreeNode();

                game.ApplyMove(move);
                outcome = children[move].simulate(game);
            }
            else if (expandedMoves.Count == 0)
            {
                Debug.Assert(game.isGameOver());
                outcome = 1 - game.GetWinner();
            }
            else
            {
                // TODO: Optimize this selection.
                double bestCriterion = -1;
                int bestMove = -1;
                double logNumEvals = Math.Log(numEvals);

                for (int move = 0; move < children.Length; ++move)
                {
                    if (children[move] != null)
                    {
                        // UCB1 selection strategy.
                        double mean = children[move].GetMean();

                        // Branchless version of
                        // if (player == 1) mean = 1 - mean;
                        mean = mean + player * (1 - 2 * mean);

                        double criterion = mean +
                            Math.Sqrt(2.0 * logNumEvals / children[move].GetNumEvals());

                        if (criterion > bestCriterion)
                        {
                            bestCriterion = criterion;
                            bestMove = move;
                        }
                    }
                }
                Debug.Assert(bestMove != -1);

                game.ApplyMove(bestMove);
                outcome = children[bestMove].select(game);
            }

            numEvals++;
            totalOutcome += outcome;

            return outcome;
        }

        private int simulate(Game game)
        {
            // Catchup moves in simulation can be disabled here.
            // game.AllowCatchups = false;

            // Get the empty hexes where moves can be made. Filling them in will finish the game.
            List<int> emptyLocs = game.getLegalMoves(false);
            shuffle(emptyLocs);

            while (!game.isGameOver())
            {
                Debug.Assert(emptyLocs.Count > 0);
                game.ApplyMove(popMove(emptyLocs));
            }
            Debug.Assert(emptyLocs.Count == 0);

            int outcome = 1 - game.GetWinner();

            numEvals++;
            totalOutcome += outcome;

            return outcome;
        }

        private static void shuffle(List<int> moves)
        {
            for (int i = 1; i < moves.Count; ++i)
            {
                int j = MCTSAI.rng.Next(0, i + 1);
                int t = moves[i];
                moves[i] = moves[j];
                moves[j] = t;
            }
        }

        private static int popRandomMove(List<int> moves)
        {
            int moveIndex = MCTSAI.rng.Next(moves.Count);
            int move = moves[moveIndex];
            moves[moveIndex] = moves[moves.Count - 1];
            moves.RemoveAt(moves.Count - 1);
            return move;
        }

        private static int popMove(List<int> moves)
        {
            int move = moves[moves.Count - 1];
            moves.RemoveAt(moves.Count - 1);
            return move;
        }

        public double GetMean()
        {
            return totalOutcome / (double)numEvals;
        }

        public int GetNumEvals()
        {
            return numEvals;
        }

        public bool AnyExpanded()
        {
            return expandedMoves != null && expandedMoves.Count > 0;
        }

        // Gets best move when invert=false.
        // Gets worst move when invert=true.
        private int getExtremeMove(bool invert)
        {
            Debug.Assert(AnyExpanded());

            double bestSubjectiveMean = -1;
            int extrLoc = -1;

            foreach (int loc in expandedMoves)
            {
                double mean = children[loc].GetMean();
                if (player == 1) mean = 1 - mean;
                if (invert) mean = 1 - mean;

                if (mean > bestSubjectiveMean)
                {
                    bestSubjectiveMean = mean;
                    extrLoc = loc;
                }
            }

            Debug.Assert(extrLoc != -1);
            return extrLoc;
        }

        public int GetBestMove()
        {
            return getExtremeMove(false);
        }

        public int GetRobustMove()
        {
            Debug.Assert(AnyExpanded());

            int bestNumEvals = -1;
            int robustLoc = -1;

            foreach (int loc in expandedMoves)
            {
                int numEvals = children[loc].GetNumEvals();

                if (numEvals > bestNumEvals)
                {
                    bestNumEvals = numEvals;
                    robustLoc = loc;
                }
            }

            Debug.Assert(robustLoc != -1);
            return robustLoc;
        }

        public int GetWorstMove()
        {
            return getExtremeMove(true);
        }

        public int GetPlayer()
        {
            return player;
        }

        public MCTSTreeNode GetChild(int loc)
        {
            return children[loc];
        }

        // Print hex grid showing all child means.
        public void DumpChildMeans()
        {
            for (int y = 0; y < Game.maxY; ++y)
            {
                int minX = Math.Max(0, y - (Game.S - 1));
                int leftNumSpaces = 6 * minX + 3 * (Game.S - 1 - y);
                String leftSpaces = new String(' ', leftNumSpaces);
                Console.Write("| " + leftSpaces);

                for (int x = 0; x < Game.maxX; ++x)
                {
                    if (!Game.inBounds(x, y)) continue;
                    var child = children[Game.toLoc(x, y)];
                    if (child != null)
                    {
                        Console.Write(" {0,4:F2} ", child.GetMean());
                    }
                    else
                    {
                        Console.Write("  null  ");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
