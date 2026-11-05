using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 손세탁 작업. targetA = 세탁대, targetB = 옷, targetC = 비누(선택).
    /// 옷이 바닥·재고에 있으면 집어서 세탁대 앞으로 옮기고, 끝나면 그 자리에 내려놓는다.
    /// 옷이 이미 착용 중이면(자기 옷) 그대로 입은 채로 씻는다. 의복은 절대 사라지지 않는다.
    /// </summary>
    public class JobDriver_WashClothing : JobDriver
    {
        private const TargetIndex StationInd = TargetIndex.A;
        private const TargetIndex GarmentInd = TargetIndex.B;
        private const TargetIndex SoapInd = TargetIndex.C;

        private Thing Garment
        {
            get { return job.GetTarget(GarmentInd).Thing; }
        }

        private CompLaundryStation Station
        {
            get
            {
                ThingWithComps withComps = job.GetTarget(StationInd).Thing as ThingWithComps;
                return withComps != null ? withComps.TryGetComp<CompLaundryStation>() : null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.GetTarget(StationInd), job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(job.GetTarget(GarmentInd), job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            // 비누는 없어도 세탁은 된다(부분 세탁). 예약 실패는 작업 실패가 아니다.
            if (job.GetTarget(SoapInd).HasThing && !pawn.Reserve(job.GetTarget(SoapInd), job, 1, -1, null, false))
            {
                job.targetC = LocalTargetInfo.Invalid;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(StationInd);
            this.FailOnDestroyedOrNull(GarmentInd);

            Thing garment = Garment;
            bool alreadyCarried = garment != null && pawn.carryTracker.CarriedThing == garment;

            if (!alreadyCarried)
            {
                yield return Toils_Goto.GotoThing(GarmentInd, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(GarmentInd, false, false, false, false);
            }

            yield return Toils_Goto.GotoThing(StationInd, PathEndMode.Touch);

            yield return Toils_General.WaitWith(StationInd, WorkTicks(), true).FailOnDestroyedOrNull(StationInd);

            yield return Toils_General.Do(FinishWash);

            yield return DropGarment();
        }

        /// <summary>빨래한 옷을 발밑에 내려놓는다. 이미 없으면 조용히 넘어간다(로그 오류 방지).</summary>
        private Toil DropGarment()
        {
            Toil toil = ToilMaker.MakeToil("DropGarment");
            toil.initAction = delegate
            {
                if (pawn.carryTracker.CarriedThing == null)
                {
                    return;
                }

                Thing dropped;
                pawn.carryTracker.TryDropCarriedThing(pawn.PositionHeld, ThingPlaceMode.Near, out dropped);
            };
            return toil;
        }

        private int WorkTicks()
        {
            CompLaundryStation station = Station;
            float work = station != null ? station.Props.workAmount : 1200f;
            float speed = pawn.GetStatValue(StatDefOf.WorkSpeedGlobal);
            if (speed < 0.1f)
            {
                speed = 0.1f;
            }

            int ticks = Mathf.RoundToInt(work / speed);
            return ticks < 60 ? 60 : ticks;
        }

        private void FinishWash()
        {
            Thing garment = Garment;
            if (garment == null || garment.Destroyed)
            {
                return;
            }

            CompLaundryStation station = Station;
            CompProperties_LaundryStation props = station != null ? station.Props : null;
            float water = props != null ? props.waterUnits : 1f;

            if (!LaundryUtility.ConsumeWater(water))
            {
                return;
            }

            bool usedSoap = false;
            if (station != null && station.Props.soapUnits > 0f)
            {
                Thing soap = job.GetTarget(SoapInd).Thing;
                bool haveSoap = soap != null && !soap.Destroyed;
                int bars = station.ClaimSoap();
                if (haveSoap)
                {
                    usedSoap = true;
                    if (bars > 0)
                    {
                        LaundryUtility.ConsumeSoap(soap, bars);
                    }
                }
                else
                {
                    station.CancelSoapClaim(bars);
                }
            }

            LaundryUtility.ApplyWash(garment, garment.TryGetComp<ThingComp_ClothingHygiene>(), usedSoap);
        }
    }
}
