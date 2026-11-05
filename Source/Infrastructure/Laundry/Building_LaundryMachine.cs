using System.Collections.Generic;
using ClothingHygiene.Domain;
using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 세탁기: 옷을 **안에 넣어 두면** 사이클마다 한 벌씩 세탁하는 자동 건물.
    ///
    /// 이전 구현은 반경 안에 놓인 옷의 오염도만 되돌려서 "집어삼키는" 느낌이었다.
    /// 지금은 내부 수납(ThingOwner)을 갖고, 넣기/꺼내기는 폰의 운반 작업이 맡는다
    /// (CH_LoadWasher / CH_UnloadWasher, 작업 유형 Hauling).
    /// 옷은 사라지지 않고 오염도만 되돌아간다.
    /// </summary>
    public class Building_LaundryMachine : Building, IThingHolder
    {
        public ThingOwner innerContainer;

        private int progress;

        /// <summary>가동 중이면 문이 잠긴다(옷을 더 넣을 수 없다).</summary>
        private bool running;

        /// <summary>마지막으로 옷이 들어온 뒤 흐른 틱. startDelayTicks 를 넘기면 가동 시작.</summary>
        private int idleWait;

        public Building_LaundryMachine()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        private Thing Current
        {
            get
            {
                return innerContainer != null && innerContainer.Count > 0 ? innerContainer[0] : null;
            }
        }

        public int Capacity
        {
            get
            {
                CompLaundryStation station = GetComp<CompLaundryStation>();
                return station != null ? station.Props.capacity : 5;
            }
        }

        public bool HasSpace
        {
            get { return innerContainer != null && innerContainer.Count < Capacity && !running; }
        }

        public bool IsRunning
        {
            get { return running; }
        }

        private bool HasDirtyGarment
        {
            get
            {
                if (innerContainer == null)
                {
                    return false;
                }

                for (int i = 0; i < innerContainer.Count; i++)
                {
                    ThingComp_ClothingHygiene comp = innerContainer[i].TryGetComp<ThingComp_ClothingHygiene>();

                    if (comp != null && comp.Filth >= HygieneValue.CleanThreshold)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasCleanGarment
        {
            get
            {
                if (innerContainer == null)
                {
                    return false;
                }

                for (int i = 0; i < innerContainer.Count; i++)
                {
                    ThingComp_ClothingHygiene comp = innerContainer[i].TryGetComp<ThingComp_ClothingHygiene>();

                    if (comp != null && comp.Filth < HygieneValue.CleanThreshold)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool TryAdd(Thing garment)
        {
            if (!HasSpace || garment == null)
            {
                return false;
            }

            return innerContainer.TryAdd(garment, false);
        }

        public Thing TakeFirstClean()
        {
            if (innerContainer == null)
            {
                return null;
            }

            for (int i = 0; i < innerContainer.Count; i++)
            {
                ThingComp_ClothingHygiene comp = innerContainer[i].TryGetComp<ThingComp_ClothingHygiene>();

                if (comp != null && comp.Filth < HygieneValue.CleanThreshold)
                {
                    return innerContainer.Take(innerContainer[i]);
                }
            }

            return null;
        }

        protected override void Tick()
        {
            base.Tick();

            CompLaundryStation station = GetComp<CompLaundryStation>();

            if (station == null || station.Props.cycleTicks <= 0)
            {
                return;
            }

            if (!station.Powered || !LaundryUtility.WaterAvailable() || !HasDirtyGarment)
            {
                progress = 0;
                running = false;
                idleWait = 0;
                return;
            }

            // 아직 가동 전: 이 창(window) 동안만 옷을 더 넣을 수 있다.
            if (!running)
            {
                idleWait++;

                if (idleWait < station.Props.startDelayTicks)
                {
                    return;
                }

                running = true;
                idleWait = 0;
                progress = 0;
                return;
            }

            progress++;

            if (progress < station.Props.cycleTicks)
            {
                return;
            }

            progress = 0;
            WashOne(station);
        }

        /// <summary>맨 앞의 옷 한 벌을 세탁한다. 물·비누는 완료 시점에 소모한다.</summary>
        private void WashOne(CompLaundryStation station)
        {
            Thing garment = Current;

            if (garment == null)
            {
                return;
            }

            CompProperties_LaundryStation props = station.Props;

            if (!LaundryUtility.ConsumeWater(props.waterUnits))
            {
                return;
            }

            bool usedSoap = props.soapUnits > 0f && LaundryUtility.TryConsumeSoapFromMap(Map, station.ClaimSoap());

            LaundryUtility.ApplyWash(garment, garment.TryGetComp<ThingComp_ClothingHygiene>(), usedSoap);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "ch_washerInner", this);
            Scribe_Values.Look(ref progress, "ch_washerProgress", 0);
            Scribe_Values.Look(ref running, "ch_washerRunning", false);
            Scribe_Values.Look(ref idleWait, "ch_washerIdleWait", 0);

            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            if (innerContainer != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
            }
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            int count = innerContainer != null ? innerContainer.Count : 0;
            string state = running ? "CH_Laundry_Running".Translate() : "CH_Laundry_Open".Translate();
            string line = "CH_WasherLoad".Translate(count.ToString(), Capacity.ToString()) + " · " + state;

            return text.NullOrEmpty() ? line : text + "\n" + line;
        }
    }
}
