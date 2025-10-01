using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matchmaking
{
    internal sealed class Match
    {
        public int requiredPlayers { get; private set; }
        public int currentPlayersAmount { get { return currentPlayers.Count; } }
        public List<Player> currentPlayers { get; private set; }


        public Match(int playerAmount)
        {
            requiredPlayers = playerAmount;
            currentPlayers = new();
        }
    }
}
