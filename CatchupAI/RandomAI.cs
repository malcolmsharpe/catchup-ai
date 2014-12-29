using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatchupAI
{
    class RandomAI
        : IPlayer
    {
        Random rng = new Random();

        public void play(Game game)
        {
            List<int> legals = game.getLegalMoves(true);

            int idx = rng.Next(legals.Count);
            int loc = legals[idx];

            game.ApplyMove(loc);
        }
    }
}
