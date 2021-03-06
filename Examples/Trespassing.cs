using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using CalloutAPI;

namespace Trespassing
{
    [CalloutProperties("Trespassing", "1.0", Probability.High)]
    public class Trespassing : CalloutAPI.Callout
    {
        /* Define spawnables here (outside methods, globally) */
        Ped Attacker;
        Ped Victim;

        Random random = new Random();

        internal class WeaponChance
        {
            public int Entry;
            public WeaponHash Weapon;
        }
        WeaponChance[] Weapons = new WeaponChance[]
        {
            new WeaponChance{ Entry = 199,Weapon = WeaponHash.Pistol},
            new WeaponChance{ Entry = 9,Weapon = WeaponHash.CombatPistol},
            new WeaponChance{ Entry = 1,Weapon = WeaponHash.CarbineRifle},
            new WeaponChance{ Entry = 8,Weapon = WeaponHash.Dagger},
            new WeaponChance{ Entry = 20,Weapon = WeaponHash.Bat},
            new WeaponChance{ Entry = 15,Weapon = WeaponHash.Knife},
        };

        public Trespassing()
        {
            /* Called when the callout is displayed */
            /* How far the callout should be? let's say random by default, you can change it */

            /* NOTE: Do not ever place spawning here!! It'll be added to the callout queue, so the constructor will be called */

            /* Randomize callout distance */

            this.InitBase(new Vector3(-935,196,67));

            this.ShortName = "Trespassing";
            this.CalloutDescription = "We've received a report that there is a trespassing in progress. Respond in code 2.";
            this.ResponseCode = 2;

            /* How close the player needs to be to start the action (OnStart())*/
            this.StartDistance = 20f; // 30 feet? metres? unit...
        }
        public async override Task Init()
        {
            /* Called when the callout is accepted */
            /* Blip spawn happens in base.OnAccept() */
            this.OnAccept();

            /* Use the SpawnPed or SpawnVehicle method to get a properly networked ped (react to other players) */
            Attacker = await SpawnPed(PedHash.ChinGoonCutscene, this.Location,12);

            Victim = await SpawnPed(PedHash.Bevhills01AFY, new Vector3(-936,202,67),170);

            /* Set ped behaviour */
            Attacker.Weapons.Give(this.RollWeapon(),250,true,true);

            Attacker.BlockPermanentEvents = true;
            Victim.BlockPermanentEvents = true;

        }
        public override void OnStart(Ped player)
        {
            /* Called when a player gets in range */

            base.OnStart(player); // -> to remove the blip from the map (yellow circle by default)

            int x = random.Next(1, 100 + 1);
            if(x <= 40)
            {
                this.Attack(player);
            }
            else if(x > 40 && x <= 65)
            {
                this.Flee(player);
            }
            else
            {
                this.Surrender();
            }
        }

        public async void Flee(Ped player)
        {
            this.Attacker.Task.FleeFrom(player);

            await BaseScript.Delay(random.Next(5500,7500));
            int x = random.Next(1, 100 + 1);

            TaskSequence sequence = new TaskSequence();
            bool changedTask = false;
            if(x <= 30)
            {
                /* 30 % the closest ped */
                sequence.AddTask.FightAgainst(GetClosestPed(this.Attacker));
                sequence.AddTask.FleeFrom(player);
                changedTask = true;
            }
            else if(x > 30 && x < 50)
            {
                /* 20% to attack the player */
                sequence.AddTask.FightAgainst(player);
                sequence.AddTask.FleeFrom(player);
                changedTask = true;
            }
            sequence.Close();

            if(changedTask)
            {
                
                ClearPedTasks(this.Attacker.Handle);
                ClearPedTasksImmediately(this.Attacker.Handle);

                this.Attacker.Task.PerformSequence(sequence);
            }
            Tick += RandomBehaviour;
        }
        public async Task RandomBehaviour()
        {
            await BaseScript.Delay(random.Next(4000,6500));

            int x = random.Next(1, 100 + 1);
            if(x <= 25)
            {
                ClearPedTasks(this.Attacker.Handle);
                ClearPedTasksImmediately(this.Attacker.Handle);

                this.Attacker.Task.FightAgainst(GetClosestPed(Attacker));
            }
            else if(x > 25 && x <= 40)
            {
                ClearPedTasks(this.Attacker.Handle);
                ClearPedTasksImmediately(this.Attacker.Handle);

                this.Attacker.Task.ReactAndFlee(GetClosestPed(Attacker));
            }
            Debug.WriteLine("In tick event");
        }

        private Ped GetClosestPed(Ped p)
        {
            Ped[] all = World.GetAllPeds();
            if (all.Length == 0)
                return null;
            float closest = float.MaxValue;
            Ped close = null;
            foreach (Ped ped in all)
            {
                if (Game.PlayerPed == ped)
                {
                    continue;
                }
                float distance = World.GetDistance(ped.Position, p.Position);
                if (distance < closest)
                {
                    close = ped;
                    closest = distance;
                }
            };
            return close;
        }

        public void Attack(Ped player)
        {
            int x = random.Next(1, 100 + 1);

            TaskSequence sequence = new TaskSequence();            

            /* 60% to attack the victim - 40 % to attack the player */
            if(x <= 60)
            {
                sequence.AddTask.FightAgainst(Victim);

                x = random.Next(1, 100 + 1);
                if (x <= 50)
                    sequence.AddTask.FightAgainst(player);
                else
                    sequence.AddTask.FleeFrom(player);
            }
            else
            {
                sequence.AddTask.FightAgainst(player);
                sequence.AddTask.FleeFrom(player);
            }
            sequence.Close();
            Attacker.Task.PerformSequence(sequence);
        }
        public void Surrender()
        {
            Attacker.Task.HandsUp(-1);
        }
        public WeaponHash RollWeapon()
        {
            int overall = 0;
            foreach (var elem in this.Weapons)
                overall += elem.Entry;

            int x = random.Next(1, overall);
            overall = 0;
            for(int i=0;i<this.Weapons.Length;i++)
            {
                overall += this.Weapons[i].Entry;
                if (overall > x)
                    return this.Weapons[i].Weapon;
            }
            return default;
        }
    }
}
