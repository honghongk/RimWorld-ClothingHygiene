# TODO — Clothing Hygiene

범례: `[ ]` 미완 · `[x]` 완료(검증 근거 있음) · `[~]` 구현·빌드 통과, **인게임 미검증**

> 2026-11-06 기준: M1~M6b 코드/def 구현 완료, 통합 빌드 0경고 0오류.
> 인게임에서 확인된 것: 모드 로딩·패치 적용(오류 0) · 오염도 누적·`냄새남` 무드 발동 · 장비 탭 표시.
> 나머지 동작(무드 보너스 중첩, 세탁 수행, 자동 갈아입기, DBH 물)은 아직 미검증.

---

## M0. 기획·규칙 문서 — 완료
- [x] `docs/PLAN.md` — 시스템 명세·밸런스 초기값·아키텍처·성능 예산
- [x] `AGENTS.md` — Karpathy 4원칙 + 중복확인 게이트 + DDD 경계 + 틱 성능 규칙
- [x] `README.md` — 기능·연구 트리·물 연동·성능·빌드

## M1. 데이터 계층 — 완료(인게임 미검증)
- [x] `ThingComp_ClothingHygiene` — **의복 1벌당** 오염도 0~1(`ch_filth`), Scribe 저장
- [x] `HygieneGameComponent` — 250틱 가드, `AllPawnsSpawned` 무할당 순회
- [x] inspect 문자열 + 한/영 Keyed
- [x] `Patches/Hygiene_ApparelComps.xml` — 코어 추상 부모 `ApparelNoQualityBase` 에 comp
- [~] 인게임 로딩·패치 적용 → 로그에 xpath/class 오류 0건 확인 필요

## M2. Domain 분리 — 완료(빌드 기준)
- [x] `Domain/{HygieneValue,HygieneStage,FilthRates,IHygieneState,IWaterSource,GarmentChangeRule,WaterMode}`
- [x] `Application/HygieneTickUseCase`
- [x] `Infrastructure/{ThingComp,CompProperties,HygieneGameComponent,ActivityReader,HygieneMod}`
- [x] 기본값 **65%/일**, 슬라이더 10~200 → 50% 도달: 유휴 18.5h / 노동 12.3h / 격렬 7.4h
- [x] `grep "using Verse\|using RimWorld" Source/Domain/` → **0건**

## M3. 활동·환경 계수 — 구현(인게임 미검증)
- [x] 수면 판정 `pawn.jobs.curDriver.asleep`
- [x] **4단 신호 판정**(`ActivityReader` v2): 격한 동작(근접·채굴·진압)=2.5 / 이동·WorkGiver 작업=1.5 / **원거리 사격=1.0** / **소집·대기(`JobDef.isIdle`)=1.0**
- [x] 기온 `map.mapTemperature.OutdoorTemp` (30℃+ ×1.3, 0℃- ×0.7)
- [~] 인게임 상승률이 65%/일과 일치하는지
- [ ] 설정에 활동 배율 노출(지금은 상수)
- verify: 3배속 1일 후 오염도 ≈80%, 수면 시간대 증가폭이 노동의 절반 이하

## M4. 무드 — 구현(인게임 미검증)
- [x] `Defs/Hygiene/ThoughtDefs_Hygiene.xml` — `CH_Clean`(+1, `stackLimit` 5, `stackedEffectMultiplier` 1), `CH_Smelly`(−3)·`CH_Clammy`(−6)·`CH_Filthy`(−10)
- [x] `HygieneMoodComponent` — 250틱, 우리 thought 4종만 제거 후 재부여(다른 모드 thought 무접촉)
- [x] 보너스 = 깨끗한 벌 수(상한 `min(cleanBonusCap,5)`), 디버프 = **가장 더러운 한 벌 1개만**
- [x] 배율 `moodPenaltyScale` → `Thought_Memory.moodPowerFactor`
- [x] 한/영 번역: Keyed `CH_Mood.xml` + **DefInjected**(ThoughtDef 라벨은 Keyed 로 안 먹음)
- [x] 새 게임/로드 직후 `StartedNewGame`·`LoadedGame` 훅으로 **즉시 1회 반영**(250틱 대기 제거)
- [x] 의복 표시: inspect(항상) + 툴팁(`CompTipStringExtra`) + 정보카드(`GetDescriptionPart`)
- [x] 모든 의복 def 에 comp 보장 — `ApparelCompInjector` 런타임 주입
- [~] 인게임 무드 수치·중첩 표시
- [ ] 의류 탭 행에 별도 % 컬럼 — 바닐라 `ITab_Pawn_Gear` UI 패치(Harmony) 필요, 보류
- verify: 깨끗 3벌 +3 / 5벌 +5 / 6벌도 +5, 더러운 3벌이어도 디버프 1개만

## M5. 세탁 — 구현(인게임 미검증)
- [x] **세탁기는 내부 수납형**(`ThingOwner`, 5칸) — 넣기(`CH_LoadWasher`)/꺼내기(`CH_UnloadWasher`) 운반 작업
- [x] **입은 채 세탁 제거** — 착용 중 의복은 세탁 대상에서 제외(갈아입기 → 벗은 옷 → 세탁 흐름)
- [x] **물가 세탁 자리**(`CH_WashSpot`) — `PlaceWorker_WaterEdge` 로 물 인접 배치 강제, 물 소비 0·작업량 2400
- [x] 세탁기 **가동 중 투입 잠금** — `startDelayTicks`(900) 창구 후 `running` 이면 `HasSpace=false`
- [x] `CH_WashTub`(연구 불필요) + `WorkGiver_WashClothing` + `JobDriver_WashClothing`
- [x] 결과: 세제 있으면 0, 없으면 `soaplessCleanFloor`(0.4)까지만. 내구도 `laundryDurabilityLossPercent`% 감소(최소 1)
- [x] 바닐라 Tainted 미접촉
- [x] 물/세제 없이도 동작
- [~] 인게임 작업 획득·운반·결과
- verify: 더러운 옷 → 세탁 → 0%, 내구도 −2%p, Tainted 유지

## M5b. 연구·세제·기계 — 구현(인게임 미검증)
- [x] 연구 4종: `ClothingHygiene_{Basin,Soap,Washer,UltraSonic}` (Medieval→Spacer, `Washer` 는 `Electricity` 선행)
- [x] `CH_LaundryBasin` / `CH_WashingMachine`(전력)/ `CH_UltraSonicWasher` / `CH_Soap` + `CH_MakeSoap`
- [x] def 라벨 번역은 DefInjected 5종(Thing/Recipe/Research/Job/WorkGiver) 한·영
- [~] 건설·연구 트리 노출, 자동 사이클, 비누 소모
- [ ] 텍스처가 **바닐라 임시 차용**(TableButcher 등) — 실제 아트 필요

## M5c. 물 — DBH 소프트 의존 — 구현(인게임 미검증)
- [x] 활성 감지 `ModLister.GetActiveModWithIdentifier("Dubwise.DubsBadHygiene")`
- [x] 리플렉션 어댑터 `DubwiseWaterSource` — **실측 확인한 실제 경로**
      `HygienePipeMapComp.DBHComps`(static Dictionary) → `PipeNets`(배열) → `PlumbingNet.WaterStorage`(프로퍼티) + `PullWater(float, out ContaminationLevel)`
- [x] `WaterSourceProvider` 분기: NoWater / Auto+DBH / Auto+DBH없음(무물 폴백) / RequireWater+물없음(Available=false)
- [x] 하드 참조 0건: `grep "using DubsBadHygiene"` 0, csproj 참조 0
- [~] 실제 DBH 배관에서 물 차감되는지(리플렉션 호출은 스텁으로만 검증)
- [ ] 물 문맥 없음(전 맵 배관망 전역 집계) — 세탁 건물 위치 기반 조회로 개선 여지
- [ ] (선택) DBH 세탁기 사이클 종료 시 우리 오염도 0 처리

## M6. 자동 갈아입기 — 구현(인게임 미검증)
- [x] `JobGiver_ChangeClothing` — `GetPriority` 5.6 (작업 5.5 위, 식사 9.5·수면 8.0 아래), 스캔은 `(TicksGame + thingIDNumber) % 250`
- [x] `Patches/Hygiene_ThinkTree.xml` — `MainColonistBehaviorCore` 의 `ThinkNode_PrioritySorter` 말미 추가(기존 노드 무접촉)
- [x] 예비복 조건: 오염 <0.2 ∧ 내구도 >50% ∧ `PawnCanWear` ∧ 예약/도달/허용구역
- [x] `Wear(spare, dropReplacedApparel:true, locked:false)` → 벗은 옷 forbid=false
- [x] **수정**: `lookInHaulSources:false → true` (바닐라 `JobGiver_OptimizeApparel` 과 동일. false 면 Deep Storage 선반의 예비복을 못 찾음)
- [~] 인게임 1회 발생 여부
- verify: 70% 방치 → 자동 교체 + 벗은 옷 바닥 / 예비복 없으면 미발급

## M6b. 아이콘 오버레이 · 활동 판정 개선
- [x] Harmony 도입(`brrainz.harmony` 의존 + csproj 참조, 0Harmony 동봉 안 함)
- [x] `ApparelIconOverlay` — `Widgets.ThingIcon` postfix, 착용 의복 좌측 세로 막대(단계별 색)
- [x] `ActivityReader` 재작성 — 수면/격한동작/**원거리 사격=평상**/소집=평상/이동·작업=노동
- [~] 장비 UI 모드가 `Widgets.ThingIcon` 을 거치는지 (직접 `GUI.DrawTexture` 면 안 잡힘 → 해당 모드 함수 추가 패치 필요)
- [ ] 의류 탭 행에 숫자 % 컬럼(바닐라 `ITab_Pawn_Gear` UI 패치) — 보류

## M5d. 연구 탭 분리 — 구현
- [x] 전용 `ResearchTabDef`(`ClothingHygiene`) — 부모 탭 `Main` 은 YART 로그상 노드 171개 규모라 4개가 묻혀 있었음
- [x] 한글 탭 이름 `의류 위생`(DefInjected/ResearchTabDef), 프로젝트 좌표 0~3 재배치
- [~] YART 탭 목록에 `ClothingHygiene` 이 뜨는지

## M7. 성능 — 계측만 구현, 측정 미완
- [x] `HygieneProfiler` — Stopwatch, 250틱 구간 누적, 게임 하루 1줄 로그
- [ ] 인게임 실측(평균/최대 µs) 수집
- [ ] DPA 로 전/후 비교(동일 세이브·배속)
- [ ] 폰 100명 규모 TPS 확인
- [ ] **전맵 스캔이 2회**(오염 누적 + 무드) → 통합 순회로 합칠지 검토
- verify: 수치 표로만 보고

## M8. 마감
- [x] 한/영 번역 파일 분리(Keyed 5 + DefInjected 6)
- [ ] 번역 키 누락 0 자동 점검(코드의 Translate 키 ↔ XML 키 대조)
- [ ] 모드 제거 후 세이브 로드 오류 0
- [ ] About.xml 정리(`modVersion`, 설명, 미리보기 이미지)

---

## 미해결 결정
- [ ] 밸런스 초기값 승인: 하루 **65%** / 임계 70% / 내구도 −2%p
- [ ] DDD 4계층 유지 vs 2계층 축소
- [x] ~~DBH 세탁기 재사용 vs 자체~~ → **자체 세탁기 확정**
- [ ] 자동 갈아입기 우선순위 5.6 유지 여부(체감 확인 후)
- [ ] 1.7 대응 시점

---

## 다음 작업 (우선순위)
1. **인게임 검증 라운드** — ① 연구 탭 `의류 위생` 노출 ② 깨끗한 옷 +1 중첩(3벌 → +3) ③ 세탁통/물가 스팟에서 실제 세탁 ④ 세탁기 넣기→가동 잠금→꺼내기 ⑤ 70% 자동 갈아입기
2. **아이콘 막대 확인** — 사용 중인 장비 UI 모드가 `Widgets.ThingIcon` 을 거치는지 (안 보이면 그 모드의 draw 함수 추가 패치)
3. **텍스처** — 4개 건물 + 비누가 바닐라 임차 그래픽. 창작마당 공개 전 필수
4. **DBH 물 실측** — 배관 연결/미연결 두 세이브에서 세탁 가능 여부
5. **의류 탭 % 컬럼** — `ITab_Pawn_Gear` UI 패치(Harmony), 보류 중
6. 성능 실측(프로파일러 로그 수집) → 전맵 스캔 2회 통합 여부 결정
