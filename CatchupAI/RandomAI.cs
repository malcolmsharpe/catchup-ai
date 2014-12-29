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
            List<int> empties = new List<int>();

            for (int x = 0; x < Game.maxX; ++x)
            {
                for (int y = 0; y < Game.maxY; ++y)
                {
                    if (!Game.inBounds(x, y)) continue;

                    if (game.getStone(x, y) != Game.Stone.Empty) continue;
                    empties.Add(game.toLoc(x, y));
                }
            }

            if (game.getMayPass()) empties.Add(-1);

            int idx = rng.Next(empties.Count);
            int loc = empties[idx];

            if (loc == -1)
            {
                game.pass();
            }
            else
            {
                game.play(loc);
            }
        }
    }
}
