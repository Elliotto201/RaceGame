using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matchmaking
{
    internal sealed class Group
    {
        public List<Player> JoinedPlayers = new();
        public uint MedianSkill 
        { 
            get 
            {
                return MathUtils.Median(JoinedPlayers.Select(t => t.Skill));
            } 
        }
        public float WaitTime { get; private set; }

        public void UpdateTime(float dt)
        {
            WaitTime += dt;
        }
    }
}
