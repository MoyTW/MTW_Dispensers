using CommunityCoreLibrary;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
namespace ExtendedStorageExtended
{
    class Building_Dispenser : Building_Storage
    {
        private IntVec3 outputSlot;
        private int maxStacks = 1000;
        private Mode mode = Mode.stockpile;
        private CompHopperUser _compHopperUser;
        private CompHopper _hopper;

        private enum Mode
        {
            stockpile = 0,
            dispense
        }

        #region Properties

        public CompHopperUser CompHopperUser
        {
            get
            {
                if (_compHopperUser == null)
                {
                    _compHopperUser = this.GetComp<CompHopperUser>();
                }
                return _compHopperUser;
            }
        }

        public CompHopper Hopper
        {
            get
            {
                if (this._hopper != null && this._hopper.FindHopperUser() == this.CompHopperUser)
                {
                    Log.Message("Using stored hopper");
                    return this._hopper;
                }
                var hopper = this.CompHopperUser.FindHoppers().FirstOrDefault();
                if (hopper != null)
                {
                    this._hopper = hopper;
                } 
                return hopper;
            }
        }

        public Thing HopperThing
        {
            get
            {
                var hopper = this.Hopper;
                return hopper != null ? hopper.GetResource(hopper.GetStoreSettings().filter) : null;
            }
        }

        public Thing StoredThing
        {
            get
            {
                var hopper = this.Hopper;
                if (hopper != null)
                {
                    return (from t in Find.ThingGrid.ThingsListAt(this.outputSlot)
                            where hopper.GetStoreSettings().AllowedToAccept(t)
                            select t).FirstOrDefault();
                }
                return null;
            }
        }

        public bool StorageHasSpaceForStack
        {
            get
            {
                Thing storedThing = this.StoredThing;
                if (storedThing != null)
                {
                    int capacity = (this.maxStacks - 1) * storedThing.def.stackLimit;
                    return storedThing.stackCount <= capacity;
                }
                return false;
            }
        }

        #endregion

        #region Overrides

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            var defStacks = ((ESdef)this.def).maxStacks;
            if (defStacks > 0)
            {
                this.maxStacks = defStacks;
            }
            this.outputSlot = GenAdj.CellsOccupiedBy(this).ToList<IntVec3>().First();
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                if (this.mode == Mode.stockpile)
                {
                    this.TryStockpileItem();
                }
                else
                {
                    this.TryDispenseItem();
                }
            }
        }

        #endregion

        private void TryStockpileItem()
        {
            Thing hopperThing = this.HopperThing;
            Thing storedThing = this.StoredThing;

            if (hopperThing != null)
            {
                if (storedThing != null)
                {
                    int numTransfer = hopperThing.stackCount;
                    storedThing.stackCount += numTransfer;
                    hopperThing.stackCount -= numTransfer;
                }
                else
                {
                    Thing newStack = ThingMaker.MakeThing(hopperThing.def, hopperThing.Stuff);
                    GenSpawn.Spawn(newStack, this.outputSlot);
                    newStack.stackCount = hopperThing.stackCount;
                }

                if (!this.StorageHasSpaceForStack)
                {
                    this.mode = Mode.dispense;
                }
                hopperThing.Destroy(0);
            }
        }

        private void DispenseItem(int numTransfer, Thing hopperThing, Thing storedThing)
        {
            if (hopperThing != null && hopperThing.def == storedThing.def)
            {
                hopperThing.stackCount += numTransfer;
                storedThing.stackCount -= numTransfer;
            }
            else if (hopperThing == null)
            {
                Thing newStack = ThingMaker.MakeThing(storedThing.def, storedThing.Stuff);
                GenSpawn.Spawn(newStack, this.Hopper.Building.AllSlotCells().First()); // Assumes hopper is 1-cell
                newStack.stackCount = numTransfer;
                storedThing.stackCount -= numTransfer;
            }
        }

        private void TryDispenseItem()
        {
            if (this.Hopper == null) { return; }

            Thing hopperThing = this.HopperThing;
            Thing storedThing = this.StoredThing;

            int hopperStackCount = hopperThing != null ? hopperThing.stackCount : 0;

            if (storedThing != null)
            {
                int numTransfer = storedThing.def.stackLimit - hopperStackCount;

                if (numTransfer >= storedThing.stackCount)
                {
                    this.mode = Mode.stockpile;
                }
                else if (numTransfer > 0)
                {
                    this.DispenseItem(numTransfer, hopperThing, storedThing);
                }
            }
            else
            {
                this.mode = Mode.stockpile;
            }
        }
    }
}
