using CommunityCoreLibrary;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
namespace ExtendedStorageExtended
{

    public class Building_ExtendedStorageExtended : Building_Storage
    {
        private IntVec3 inputSlot;
        private IntVec3 outputSlot;
        private IntVec3 beforeInput;
        private int maxStorage = 1000;
        private ThingDef storedThingDef;
        private Mode mode = Mode.stockpile;
        private CompHopperUser compHopperUser;

        private enum Mode
        {
            stockpile = 0,
            dispense
        }

        public void FindAndSynchronizeHoppers(StorageSettings settings)
        {
            var hoppers = this.CompHopperUser.FindHoppers();
            if (hoppers.NullOrEmpty())
            {
                return;
            }
            foreach (var hopper in hoppers)
            {
                hopper.DeprogramHopper();
                hopper.ProgramHopper(settings);
            }
        }

        public ThingFilter ResourceFilter
        {
            get
            {
                return this.GetStoreSettings().filter;
            }
        }

        public CompHopperUser CompHopperUser
        {
            get
            {
                if (compHopperUser == null)
                {
                    compHopperUser = this.GetComp<CompHopperUser>();
                }
                return compHopperUser;
            }
        }

        public Thing StoredThingAtInput
        {
            get
            {
                if (this.storedThingDef != null)
                {
                    List<Thing> list = (
                        from t in Find.ThingGrid.ThingsAt(this.inputSlot)
                        where t.def == this.storedThingDef
                        select t).ToList<Thing>();
                    if (list.Count <= 0)
                    {
                        return null;
                    }
                    return list.First<Thing>();
                }
                else
                {
                    List<Thing> list2 = (
                        from t in Find.ThingGrid.ThingsAt(this.inputSlot)
                        where this.slotGroup.Settings.AllowedToAccept(t)
                        select t).ToList<Thing>();
                    if (list2.Count <= 0)
                    {
                        return null;
                    }
                    return list2.First<Thing>();
                }
            }
        }
        public Thing StoredThing
        {
            get
            {
                if (this.storedThingDef == null)
                {
                    return null;
                }
                List<Thing> list = (
                    from t in Find.ThingGrid.ThingsListAt(this.outputSlot)
                    where t.def == this.storedThingDef
                    select t).ToList<Thing>();
                if (list.Count <= 0)
                {
                    return null;
                }
                return list.First<Thing>();
            }
        }
        public bool StorageFull
        {
            get
            {
                return this.storedThingDef != null && this.StoredThing != null && this.StoredThing.stackCount >= this.ApparentMaxStorage;
            }
        }
        public int ApparentMaxStorage
        {
            get
            {
                if (this.storedThingDef == null)
                {
                    return 0;
                }
                if (this.storedThingDef.smallVolume)
                {
                    return (int)((float)this.maxStorage / 0.2f);
                }
                return this.maxStorage;
            }
        }
        public override void SpawnSetup()
        {
            base.SpawnSetup();
            this.maxStorage = ((ESdef)this.def).maxStorage;
            List<IntVec3> list = GenAdj.CellsOccupiedBy(this).ToList<IntVec3>();
            this.inputSlot = list[0];
            this.outputSlot = list[1];

            IntVec2 s = new IntVec2(1, 1);
            this.beforeInput = GenAdj.CellsAdjacentCardinal(this.inputSlot, this.Rotation, s).First();
        }
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                Log.Message("Rotation: " + this.Rotation + " inputSlot: " + this.inputSlot + " outputSlot:" + this.outputSlot + " adjInput: " + this.beforeInput);

                // TODO: Change to synchronize on change in parent settings only!
                if (Find.TickManager.TicksGame % 120 == 0)
                {
                    this.FindAndSynchronizeHoppers(this.GetStoreSettings());
                }

                this.CheckOutputSlot();
                if (this.mode == Mode.stockpile)
                {
                    this.TryMoveItem();
                }
                else
                {
                    Thing storedThingAtInput = this.StoredThingAtInput;
                    if (storedThingAtInput == null)
                    {
                        this.mode = Mode.stockpile;
                    }
                    else if (storedThingAtInput != null && storedThingAtInput.stackCount <= storedThingAtInput.def.stackLimit)
                    {
                        this.mode = Mode.stockpile;
                        this.StoredThing.SetForbidden(false);
                    }
                }
            }
        }
        private void CheckOutputSlot()
        {
            if (this.storedThingDef == null)
            {
                return;
            }
            if (this.StoredThing == null)
            {
                this.storedThingDef = null;
                return;
            }
            List<Thing> list = (
                from t in Find.ThingGrid.ThingsAt(this.outputSlot)
                where t.def == this.storedThingDef
                orderby t.stackCount
                select t).ToList<Thing>();
            if (list.Count > 1)
            {
                Thing thing = ThingMaker.MakeThing(this.storedThingDef, list.First<Thing>().Stuff);
                foreach (Thing current in list)
                {
                    thing.stackCount += current.stackCount;
                    current.Destroy(0);
                }
                GenSpawn.Spawn(thing, this.outputSlot);
            }
        }
        private void TryMoveItem()
        {
            if (this.storedThingDef == null)
            {
                Thing storedThingAtInput = this.StoredThingAtInput;
                if (storedThingAtInput != null)
                {
                    this.storedThingDef = storedThingAtInput.def;
                    Thing thing = ThingMaker.MakeThing(this.storedThingDef, storedThingAtInput.Stuff);
                    thing.stackCount = storedThingAtInput.stackCount;
                    storedThingAtInput.Destroy(0);
                    GenSpawn.Spawn(thing, this.outputSlot);
                }
                return;
            }
            Thing storedThingAtInput2 = this.StoredThingAtInput;
            Thing storedThing = this.StoredThing;
            if (storedThingAtInput2 != null)
            {
                if (storedThing != null)
                {
                    int a = this.ApparentMaxStorage - storedThing.stackCount;
                    int num = Mathf.Min(a, storedThingAtInput2.stackCount);
                    storedThing.stackCount += num;
                    storedThingAtInput2.stackCount -= num;
                    if (this.StorageFull)
                    {
                        int n = storedThing.stackCount - storedThing.def.stackLimit;
                        storedThingAtInput2.stackCount += n;
                        storedThing.stackCount -= n;
                        storedThing.SetForbidden(true);

                        this.mode = Mode.dispense;
                    }
                    else if (storedThingAtInput2.stackCount <= 0)
                    {
                        storedThingAtInput2.Destroy(0);
                    }

                    return;
                }
                Thing thing2 = ThingMaker.MakeThing(storedThingAtInput2.def, storedThingAtInput2.Stuff);
                GenSpawn.Spawn(thing2, this.outputSlot);
                storedThingAtInput2.Destroy(0);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.LookDef<ThingDef>(ref this.storedThingDef, "storedThingDef");
        }
    }
}
