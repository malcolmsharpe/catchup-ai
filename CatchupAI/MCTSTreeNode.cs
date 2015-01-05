using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CatchupAI
{
    class MCTSTreeNode
    {
        private struct AIAction
        {
            public int move;
        }

        const double EPS = 1e-8;
        const double RaveK = 1000; // TODO: why?
        const double RaveC = 2.0; // ...?

        MCTSAI ai;

        int numEvals = 0;
        int totalOutcome = 0;

        int numRave = 0;
        int totalRave = 0;

        int player;
        AIAction[] actions;
        MCTSTreeNode[] children;

        public MCTSTreeNode(MCTSAI ai)
        {
            this.ai = ai;
        }

        public void ApplyRaveOutcome(int outcome)
        {
            ++numRave;
            totalRave += outcome;
        }

        // Returns the outcome.
        // 1: Black win
        // 0: White win
        public int select(Game game)
        {
            if (actions == null)
            {
                // Initializing is delayed until first selection.
                player = game.getCurrentPlayer();

                List<int> moves = game.getLegalMoves(true);
                actions = new AIAction[moves.Count];
                Debug.Assert(children == null);
                children = new MCTSTreeNode[moves.Count];

                for (int i = 0; i < moves.Count; ++i)
                {
                    actions[i].move = moves[i];
                    children[i] = new MCTSTreeNode(ai);
                }
            }

            // TODO: Optimize this selection.
            double bestCriterion = -1;
            int bestI = -1;
            int numBest = 0;
            double logNumEvals = Math.Log(numEvals);

            int childNumRave = 0;
            for (int i = 0; i < children.Length; ++i)
            {
                childNumRave += children[i].numRave;
                ai.putNodeInRaveTable(children[i], game.getCurrentPlayer(), actions[i].move);
            }

            for (int i = 0; i < children.Length; ++i)
            {
                double criterion = children[i].GetMixedCriterion(player, numEvals, logNumEvals, childNumRave);

                if (criterion > bestCriterion)
                {
                    bestCriterion = criterion;
                    bestI = i;
                    numBest = 1;
                }
                else if (criterion + EPS >= bestCriterion)
                {
                    Debug.Assert(numBest >= 1);
                    ++numBest;

                    // Every equal candidate should be selected with equal probability.
                    // Note that in the early phases when each child has been evaluated
                    // either zero or a small number of times, ties will in fact occur
                    // most of the time. For example, every child that has never been
                    // visited has equal criterion.
                    if (MCTSAI.rng.Next(numBest) == 0)
                    {
                        bestCriterion = criterion;
                        bestI = i;
                    }
                }
            }

            int outcome;
            if (bestI == -1)
            {
                // This is the case where there is no child, i.e., there is no legal move.
                Debug.Assert(game.isGameOver());
                outcome = 1 - game.GetWinner();
            }
            else
            {
                game.ApplyMove(actions[bestI].move);

                if (children[bestI].GetNumEvals() == 0)
                {
                    outcome = children[bestI].simulate(game);
                }
                else
                {
                    outcome = children[bestI].select(game);
                }
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

            int[] turnPlayer = new int[emptyLocs.Count];
            for (int i = 0; i < emptyLocs.Count; ++i)
            {
                Debug.Assert(!game.isGameOver());
                turnPlayer[i] = game.getCurrentPlayer();
                game.ApplyMove(emptyLocs[i]);
            }

            int outcome = 1 - game.GetWinner();

            // Apply outcome to RAVE estimates.
            for (int i = 0; i < emptyLocs.Count; ++i)
            {
                int player = turnPlayer[i];
                int loc = emptyLocs[i];
                for (MCTSAI.NodeLink link = ai.raveTable[player][loc]; link != null; link = link.next)
                {
                    link.node.ApplyRaveOutcome(outcome);
                }
            }

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

        // Mix UCB1 and RAVE.
        public double GetMixedCriterion(int parentPlayer, int parentNumEvals,
            double parentLogNumEvals, int parentNumRave)
        {
            double ucb1 = GetUCB1Criterion(parentPlayer, parentLogNumEvals);
            if (totalRave == 0) return ucb1;

            double rave = GetRAVECriterion(parentPlayer, parentNumRave);

            double beta = Math.Sqrt(RaveK / (3*parentNumEvals + RaveK));
            return beta * rave + (1 - beta) * ucb1;
        }

        public double GetUCB1Criterion(int parentPlayer, double parentLogNumEvals)
        {
            if (numEvals == 0)
            {
                return double.PositiveInfinity;
            }

            // UCB1 selection strategy.
            double mean = GetMean();

            // Branchless version of
            // if (player == 1) mean = 1 - mean;
            mean = mean + parentPlayer * (1 - 2 * mean);

            return mean +
                Math.Sqrt(2.0 * parentLogNumEvals / GetNumEvals());
        }

        public double GetRAVECriterion(int parentPlayer, int parentNumRave)
        {
            Debug.Assert(totalRave > 0);

            // RAVE selection strategy.
            double mean = GetRAVE();
            mean = mean + parentPlayer * (1 - 2 * mean);

            return RaveC * Math.Sqrt(Math.Log(parentNumRave) / numRave);
        }

        public double GetRAVE()
        {
            Debug.Assert(totalRave > 0);
            return totalRave / (double)numRave;
        }

        public double GetMean()
        {
            Debug.Assert(numEvals > 0);
            return totalOutcome / (double)numEvals;
        }

        public int GetNumEvals()
        {
            return numEvals;
        }

        public bool AnyExpanded()
        {
            return children.Length > 0 && numEvals >= 2;
        }

        // Gets best move when invert=false.
        // Gets worst move when invert=true.
        private int getExtremeMove(bool invert)
        {
            Debug.Assert(AnyExpanded());

            double bestSubjectiveMean = -1;
            int extrLoc = -1;

            for (int i = 0; i < children.Length; ++i)
            {
                if (children[i].GetNumEvals() == 0) continue;

                double mean = children[i].GetMean();
                if (player == 1) mean = 1 - mean;
                if (invert) mean = 1 - mean;

                if (mean > bestSubjectiveMean)
                {
                    bestSubjectiveMean = mean;
                    extrLoc = actions[i].move;
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

            for (int i = 0; i < children.Length; ++i)
            {
                int numEvals = children[i].GetNumEvals();

                if (numEvals > bestNumEvals)
                {
                    bestNumEvals = numEvals;
                    robustLoc = actions[i].move;
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
            for (int i = 0; i < children.Length; ++i)
            {
                if (actions[i].move == loc)
                {
                    return children[i];
                }
            }
            Debug.Assert(false);
            return null;
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

                    MCTSTreeNode child = null;
                    for (int i = 0; i < children.Length; ++i)
                    {
                        if (actions[i].move == Game.toLoc(x, y))
                        {
                            child = children[i];
                        }
                    }

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

        public void DumpStats()
        {
            String raveStr = "n/a";
            if (numRave > 0)
            {
                raveStr = GetRAVE().ToString();
            }
            Console.WriteLine("mean {0};  visits {1};  rave {2};  raves {3}",
                GetMean(), numEvals, raveStr, numRave);
        }
    }
}
