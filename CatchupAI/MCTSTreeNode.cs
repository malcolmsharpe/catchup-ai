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
                children = new MCTSTreeNode[game.locLen + 1]; // locLen means pass

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

                for (int move = 0; move < children.Length; ++move)
                {
                    if (children[move] != null)
                    {
                        // UCB1 selection strategy.
                        double mean = children[move].GetMean();
                        if (player == 1) mean = 1 - mean;
                        double criterion = mean +
                            Math.Sqrt(2.0 * Math.Log(numEvals) / children[move].GetNumEvals());

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

        public int GetBestMove()
        {
            double bestSubjectiveMean = -1;
            int bestLoc = -1;

            foreach (int loc in expandedMoves)
            {
                double mean = children[loc].GetMean();
                if (player == 1) mean = 1 - mean;

                if (mean > bestSubjectiveMean)
                {
                    bestSubjectiveMean = mean;
                    bestLoc = loc;
                }
            }

            Debug.Assert(bestLoc != -1);
            return bestLoc;
        }
    }
}
