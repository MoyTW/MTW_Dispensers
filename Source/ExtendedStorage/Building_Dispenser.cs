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
        private CompHopperUser compHopperUser;
        private CompHopper hopper;

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
                if (compHopperUser == null)
                {
                    compHopperUser = this.GetComp<CompHopperUser>();
                }
                return compHopperUser;
            }
        }

        public CompHopper Hopper
        {
            get
            {
                if (this.hopper.FindHopperUser() == this.CompHopperUser)
                {
                    Log.Message("Using stored hopper");
                    return this.hopper;
                }
                this.hopper = this.CompHopperUser.FindHoppers().FirstOrDefault();
                return this.hopper;
            }
        }

        public Thing HopperThing
        {
            get
            {
                return this.Hopper.GetResource(Hopper.GetStoreSettings().filter);
            }
        }

        public Thing StoredThing
        {
            get
            {
                return (from t in Find.ThingGrid.ThingsListAt(this.outputSlot)
                        where this.Hopper.GetStoreSettings().AllowedToAccept(t)
                        select t).FirstOrDefault();
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
    }
}
