using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matchmaking
{
    public sealed class Matchmaker
    {
        private readonly float skillWeight;
        private readonly float timeWeight;
        private readonly uint maximumGroupScore;
        private readonly int maximumPlayersForNoSkillCheckAdd;
        private readonly uint maximumSkillDifferenceForWaitingQueuePlayer;
        private readonly int PlayersForMatch;
        private readonly float GroupAmountMultiplier;

        private readonly Dictionary<Guid, Group> playerGroupLookup;
        private readonly List<Group> activeGroups;
        private readonly Logger logger;

        private Queue<Player> playerWaitQueue = new();
        private int currentAmountPlayers;

        public event Action<Log> OnLog;
        public event Action<Player[]> OnMatchFound;

        public Matchmaker(float skillWeight, float timeWeight, uint maximumGroupScore, int maximumPlayersForNoSkillCheckAdd, uint maximumSkillDifferenceForWaitingQueuePlayer
    , int PlayersForMatch, float GroupAmountMultiplier)
        {
            logger = new Logger();
            playerGroupLookup = new();
            activeGroups = new();

            logger.OnLog += log => OnLog?.Invoke(log);

            this.skillWeight = skillWeight;
            this.timeWeight = timeWeight;
            this.maximumGroupScore = maximumGroupScore;
            this.maximumPlayersForNoSkillCheckAdd = maximumPlayersForNoSkillCheckAdd;
            this.maximumSkillDifferenceForWaitingQueuePlayer = maximumSkillDifferenceForWaitingQueuePlayer;
            this.PlayersForMatch = PlayersForMatch;
            this.GroupAmountMultiplier = GroupAmountMultiplier;
        }

        public void AddPlayer(Player player)
        {
            if (playerGroupLookup.ContainsKey(player.Id))
            {
                logger.Log("AddPlayer was called on a Player that was already in a group!", LogType.Warning);

                return;
            }

            if(activeGroups.Count == 0)
            {
                var group = new Group();
                activeGroups.Add(group);
            }

            uint bestScore = uint.MaxValue;
            Group bestGroup = null!;
            Group emptyGroup = null;

            uint previousSkillMedian = 0;

            foreach (var group in activeGroups)
            {
                if(group.JoinedPlayers.Count == 0 && emptyGroup == null)
                {
                    emptyGroup = group;
                    break;
                }
                else
                {
                    uint groupMedian = group.MedianSkill;
                    float waitTime = group.WaitTime;

                    if (bestScore == uint.MaxValue)
                    {
                        bestScore = ScoreGroup(MathUtils.Distance(player.Skill, groupMedian), waitTime);
                        bestGroup = group;
                    }
                    else if (ScoreGroup(MathUtils.Distance(groupMedian, player.Skill), waitTime) < ScoreGroup(previousSkillMedian, player.Skill))
                    {
                        bestScore = ScoreGroup(MathUtils.Distance(groupMedian, player.Skill), waitTime);
                        bestGroup = group;
                    }

                    previousSkillMedian = groupMedian;
                }
            }

            if(bestScore < maximumGroupScore)
            {
                bestGroup.JoinedPlayers.Add(player);
                playerGroupLookup.Add(player.Id, bestGroup);

                CheckGroupFull(bestGroup);

                logger.Log($"Player with skill {player.Skill} was added to matchmaking group with a score of {bestScore}", LogType.Message);
            }
            else if(emptyGroup != null)
            {
                emptyGroup.JoinedPlayers.Add(player);
                playerGroupLookup.Add(player.Id, emptyGroup);

                logger.Log($"Player with skill {player.Skill} was added to matchmaking group that was empty", LogType.Message);
            }
            else
            {
                playerWaitQueue.Enqueue(player);

                logger.Log("Player added to wait queue", LogType.Message);
            }

            currentAmountPlayers++;
        }

        public void RemovePlayer(Player player)
        {
            if(playerGroupLookup.TryGetValue(player.Id, out var group))
            {
                group.JoinedPlayers.RemoveAll(t => t.Id == player.Id);

                if(group.JoinedPlayers.Count == 0)
                {
                    playerGroupLookup.Remove(player.Id);
                }

                currentAmountPlayers--;
                logger.Log("Player was removed from matchmaking successfully", LogType.Message);
            }
            else if(playerWaitQueue.Any(t => t.Id == player.Id))
            {
                Queue<Player> newQueue = new Queue<Player>();
                while (playerWaitQueue.Count > 0)
                {
                    Player p = playerWaitQueue.Dequeue();
                    if (p.Id != player.Id)
                        newQueue.Enqueue(p);
                }
                playerWaitQueue = newQueue;

                logger.Log("Player was removed from matchmaking successfully", LogType.Message);
            }
            else
            {
                logger.Log($"Player did not exist in matchmaking that was tried to be removed", LogType.Warning);
            }
        }

        public void UpdateTime(float dt)
        {
            foreach(var group in activeGroups)
            {
                group.UpdateTime(dt);
            }
        }

        /// <summary>
        /// Should probaly be called around every second or every half a second
        /// </summary>
        public void Tick()
        {
            int neededGroupAmount = GetRoomAmount();
            if(activeGroups.Count < neededGroupAmount)
            {
                int neededExtraRooms = neededGroupAmount - activeGroups.Count;

                for(int i = 0; i < neededExtraRooms; i++)
                {
                    var group = new Group();
                    activeGroups.Add(group);
                }
            }

            if(playerWaitQueue.Count > 0)
            {
                foreach (var group in activeGroups.ToArray())
                {
                    TryAddWaitingPlayerToGroup(group);
                    CheckGroupFull(group);
                }
            }
        }

        private void TryAddWaitingPlayerToGroup(Group group)
        {
            if (playerWaitQueue.Count == 0) return;

            if (group.JoinedPlayers.Count <= maximumPlayersForNoSkillCheckAdd)
            {
                if (playerWaitQueue.TryDequeue(out var queuePlayer))
                {
                    group.JoinedPlayers.Add(queuePlayer);
                }
            }
            else
            {
                if (playerWaitQueue.TryPeek(out var queuePlayer))
                {
                    if(MathUtils.Distance(queuePlayer.Skill, group.MedianSkill) <= maximumSkillDifferenceForWaitingQueuePlayer)
                    {
                        var player = playerWaitQueue.Dequeue();

                        group.JoinedPlayers.Add(player);
                        playerGroupLookup.Add(player.Id, group);
                    }
                }
            }
        }

        private void CheckGroupFull(Group group)
        {
            if (group.JoinedPlayers.Count == PlayersForMatch)
            {
                var matchPlayers = group.JoinedPlayers.ToArray();

                group.JoinedPlayers.Clear();
                activeGroups.Remove(group);

                foreach (var _player in matchPlayers)
                {
                    playerGroupLookup.Remove(_player.Id);
                }

                OnMatchFound?.Invoke(matchPlayers);
            }
        }

        private uint ScoreGroup(uint skillDifference, float waitTime)
        {
            return (uint)(skillWeight * skillDifference + timeWeight * waitTime);
        }

        private int GetRoomAmount()
        {
            return (int)(((float)currentAmountPlayers / PlayersForMatch) * GroupAmountMultiplier);
        }
    }
}
