using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matchmaking
{
    public sealed class Player
    {
        public Guid Id { get; private set; }
        public uint Skill { get; private set; }

        public Player(Guid id, uint skill)
        {
            Id = id;
            Skill = skill;
        }
    }
}
