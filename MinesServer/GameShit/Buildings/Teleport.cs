﻿using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.WorldSystem;
using MinesServer.Network.HubEvents;
using MinesServer.Network.World;
using MinesServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit.Buildings
{
    public class Teleport : Pack, IDamagable
    {
        public DateTime brokentimer { get; set; }
        public float charge { get; set; }
        public float maxcharge { get; set; }
        public int hp { get; set; }
        public int maxhp { get; set; }
        public int cost { get; set; }
        [NotMapped]
        public override int off => charge > 0 ? 1 : 0;
        private Teleport() {}
        public Teleport(int x, int y, int ownerid) : base(x, y, ownerid, PackType.Teleport)
        {
            cost = 10;
            charge = 1000;
            maxcharge = 10000;
            hp = 1000;
            maxhp = 1000;
            using var db = new DataBase();
            db.teleports.Add(this);
            db.SaveChanges();
        }
        #region affectworld
        protected override void ClearBuilding()
        {
            World.SetCell(x, y, 37, true);
            World.SetCell(x, y + 1, 37, true);
            World.SetCell(x + 1, y, 106, true);
            World.SetCell(x + 1, y - 1, 106, true);
            World.SetCell(x + 1, y + 1, 106, true);
            World.SetCell(x - 1, y - 1, 106, true);
            World.SetCell(x - 1, y + 1, 106, true);
            World.SetCell(x, y - 1, 106, true);
            World.SetCell(x - 1, y, 106, true);
        }
        public override void Build()
        {
            World.SetCell(x, y, 37, true);
            World.SetCell(x, y + 1, 37, true);
            World.SetCell(x + 1, y, 106, true);
            World.SetCell(x + 1, y - 1, 106, true);
            World.SetCell(x + 1, y + 1, 106, true);
            World.SetCell(x - 1, y - 1, 106, true);
            World.SetCell(x - 1, y + 1, 106, true);
            World.SetCell(x, y - 1, 106, true);
            World.SetCell(x - 1, y, 106, true);
            base.Build();
        }
        public void Destroy(Player p)
        {
            ClearBuilding();
            World.RemovePack(x, y);
            if (charge > 0)
            {
                var temp =new long[] { 0, 0, 0, 0, (long)charge, 0};
                Box.BuildBox(x, y, temp,null);
            }
            using var db = new DataBase();
            db.teleports.Remove(this);
            db.SaveChanges();
            if (Physics.r.Next(1, 101) < 40)
            {
                p.connection?.SendB(new HBPacket([new HBChatPacket(0, x, y, "ШПАААК ВЫПАЛ")]));
                p.inventory[0]++;
            }
        }
        #endregion

        public override Window? GUIWin(Player p)
        {
            return null;
        }
    }
}
