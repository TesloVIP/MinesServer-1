﻿using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.WorldSystem;
using System.Diagnostics;

namespace MinesServer.Server
{
    public class ServerTime
    {
        public delegate void GameAction();
        public Queue<(GameAction action,Player initiator)> gameActions;
        private List<TickAction> actions = new();
        public ServerTime()
        {
            StartTimeUpdate();
            programmatorUpdate();
            gameActions = new Queue<(GameAction,Player)>();
        }
        public void AddAction(GameAction action,Player p)
        {
            if (ServerTime.Now < directactiondelay) return;
            gameActions.Enqueue((action,p));
            directactiondelay = Now + TimeSpan.FromMicroseconds(5);
        }
        private UpdateThread<int> players = new();
        private UpdateThread<int> chunks = new();
        private static DateTimeOffset directactiondelay = ServerTime.Now;
        public static int offset;
        public static DateTimeOffset Now { get; private set; }
        public void StartTimeUpdate()
        {

            Task.Run(() =>
            {
                while (true)
                {
                    var d = DateTimeOffset.Now;
                    offset = (int)(Now-d).TotalMicroseconds;
                    Now = d;
                    Thread.Sleep(TimeSpan.FromMicroseconds(50));
                }
            });
        }
        const int tps = 128;
        public void AddTickRateUpdate(Action body)
        {
            Task.Run(() =>
            {
                    var lasttick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    while (true)
                    {
                        int ticksToProcess = (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lasttick) / 1000f * tps);
                        if (ticksToProcess > 0)
                        {
                            if (ticksToProcess > 1)
                            {
                                Console.WriteLine($"overload {ticksToProcess}");;
                            }
                            while (ticksToProcess-- > 0) body();
                            lasttick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                    }
            });
        }
        public void programmatorUpdate()
        {
            Task.Run(() =>
            {
                while(true)
                { 
                var players = DataBase.activeplayers;
                for (int i = 0; i < players.Count; i++)
                {
                    if (players.Count > i)
                    {
                        players[i]?.ProgrammatorUpdate();
                    }
                }
                Thread.Sleep(TimeSpan.FromMicroseconds(50));
                    }
            });
        }
        public void Start() => AddTickRateUpdate(Update);
        public void Update()
        {
            if (!MServer.started)
                return;
            for (int i = 0; i < gameActions.Count; i++)
            {
                var item = gameActions.Dequeue();
                /*try
                {*/
                if (item.action != null)
                {
                    item.action();
                }
                /*}
                catch (Exception ex)
                {
                    Console.WriteLine($"{item.initiator.name}[{item.initiator.id}] caused {ex}");
                }*/
            }
            var players = DataBase.activeplayers;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player is not null)
                {
                    this.players.Enqueue(player.id, () => player.Update());
                }
            }
            this.players.processAll();
            for (int x = 0; x < World.ChunksW; x++)
            {
                for (int y = 0; y < World.ChunksH; y++)
                {
                    var lx = x;var ly = y;
                    chunks.Enqueue(ly + World.ChunksH * lx, () => World.W.chunks[lx, ly].Update());
                }
            }
            chunks.Enqueue(-1, World.Update);
            chunks.Enqueue(-2, () => {
                World.W.cells.Commit();
                World.W.road.Commit();
                World.W.durability.Commit();
            });
            chunks.processAll();
            using var db = new DataBase();
            foreach (var order in db.orders)
            {
                order.CheckReady();
            }
            db.SaveChanges();
        }
    }
}
