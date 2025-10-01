using Matchmaking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Server
{
    internal class Program
    {
        static Random rng = new Random();
        static List<Player> activePlayers = new List<Player>();

        static void Main(string[] args)
        {
            Matchmaker matchmaker = new Matchmaker(1, -13, 250, 3, 100, 10, 1.2f);
            matchmaker.OnLog += Matchmaker_OnLog;
            matchmaker.OnMatchFound += Matchmaker_OnMatchFound;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            double lastTick = 0;

            while (true)
            {
                double elapsed = sw.Elapsed.TotalSeconds;

                matchmaker.UpdateTime((float)(elapsed - lastTick));

                if (elapsed - lastTick >= 0.5)
                {
                    matchmaker.Tick();
                    lastTick = elapsed;
                }

                if (rng.NextDouble() < 0.6)
                {
                    Player p = new Player(Guid.NewGuid(), (uint)rng.Next(0, 500));
                    activePlayers.Add(p);
                    matchmaker.AddPlayer(p);
                }

                if (activePlayers.Count > 0 && rng.NextDouble() < 0.25)
                {
                    int idx = rng.Next(activePlayers.Count);
                    Player p = activePlayers[idx];
                    activePlayers.RemoveAt(idx);
                    matchmaker.RemovePlayer(p);
                }

                Thread.Sleep(100);
            }
        }

        private static void Matchmaker_OnLog(Log obj)
        {
            Console.WriteLine($"[{obj.Type}] {obj.LogMessage}");
        }

        private static void Matchmaker_OnMatchFound(Player[] players)
        {
            Console.WriteLine($"Match found with {players.Length} players!");
        }
    }
}
