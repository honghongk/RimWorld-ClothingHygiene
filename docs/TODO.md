# TODO — Clothing Hygiene

범례: `[ ]` 미완 · `[x]` 완료(검증 근거 있음) · `[~]` 빌드만 통과(인게임 미검증)

---

## M0. 기획·규칙 문서
- [x] `docs/PLAN.md` — 시스템 명세·밸런스 초기값·아키텍처·성능 예산
- [x] `AGENTS.md` — Karpathy 4원칙 + 중복확인 게이트 + DDD 경계 + 틱 성능 규칙
- verify: 두 파일 존재, PLAN 의 수치가 코드 기본값과 비교 가능한 형태

## M1. 최소 데이터 계층 (구현 완료 · 인게임 미검증)
- [x] `CompProperties_ClothingHygiene` / `ThingComp_ClothingHygiene` — 오염도 0~1 저장(`ch_filth`)
- [x] `HygieneGameComponent` — 250틱 가드, `AllPawnsSpawned` 순회, 착용 의복 누적
- [x] inspect 문자열 + 한/영 번역 키
- [x] `Patches/Hygiene_ApparelComps.xml` — 코어 추상 부모 `ApparelNoQualityBase` 에 comp 부착
- [x] `dotnet build` 0경고 0오류, 산출물 `.NETFramework,Version=v4.7.2` 확인
- [~] 인게임 로딩·패치 적용
- verify: `Player.log` 에 `Failed to find a node with the given xpath`·`Could not find class` 0건, 의복 inspect 에 `청결도` 표시

## M2. Domain 분리 + 밸런스 기본값 정렬
- [ ] `Domain/HygieneValue` — 0~1 클램프 값 객체
- [ ] `Domain/HygieneStage` — 단계 경계·라벨 (현재 ThingComp 안의 배열을 이관)
- [ ] `Domain/FilthRates` — 활동/환경/의복 계수 → 틱당 누적량
- [ ] 기본값 33% → **80%/일** (PLAN §4.2) 로 정렬, 슬라이더 범위 10~200
- [ ] `Source/Domain/` 에 `using Verse`·`using RimWorld` 0건
- verify: `grep -rn "using Verse\|using RimWorld" Source/Domain/` → 0건 / `dotnet build` 0오류 / 인게임 오염도 상승률이 80%/일과 일치(1일 3배속 관찰)

## M3. 활동·환경 계수 (API 확인 선행)
- [ ] 수면/노동/전투 판정 API 확인 — `Pawn.CurJobDef`·`pawn.Awake()`·`JobDef` 태그 조사
- [ ] 기온 판정 API 확인 — `Map.mapTemperature` 계열
- [ ] 계수 적용, 설정에 배율 노출
- verify: 3배속 1일 동안 수면 시간대 누적이 노동 시간대의 절반 이하(로그 계측으로 확인)

## M4. 무드 (ThoughtDef)
- [ ] 단계별 ThoughtDef 4개 (`CH_Smelly`·`CH_Clammy`·`CH_Filthy` + 깨끗함 보너스)
- [ ] 부여 로직: **가장 더러운 한 벌** 기준, 중복 합산 금지
- [ ] 옷을 벗거나 세탁하면 즉시 해소(`Nullifying`)
- verify: 무드 탭에서 thought 이름·수치 확인, 옷 5벌 착용 시에도 −6 (합산 −30 아님)

## M5. 세탁
- [ ] 세탁통(수동) — WorkGiver + JobDriver + `workAmount`
- [ ] 세탁기(전력) — `Electricity` 연구, 자동 반복
- [ ] 세탁 결과: 오염 0, 내구도 −2%p(설정 가능)
- [ ] 바닐라 Tainted 는 건드리지 않음
- verify: 더러운 옷 → 세탁 → inspect 0%, 내구도 −2%p, Tainted 유지

## M6. 자동 갈아입기
- [ ] 예비복 검색(재고·바닥·선반): 오염 <20% ∧ 내구도 >50% ∧ `PawnCanWear`
- [ ] `JobDef_ChangeClothing` + JobGiver 삽입 지점 조사(`ThinkTree`)
- [ ] `Pawn_ApparelTracker.Wear(new, dropReplaced:true)` 사용, 벗은 옷 forbid=false
- verify: 70% 도달 후 방치 → 폰이 스스로 갈아입고 벗은 옷이 바닥에 있음 / 예비복 없으면 갈아입지 않음

## M7. 성능 계측·검증 (틱 1순위)
- [ ] 자체 Stopwatch 계측(250틱 1회 누적, 하루 1줄 로그)
- [ ] Dubs Performance Analyzer 로 틱 비용 전후 비교(동일 세이브·배속)
- [ ] 폰 100명 규모 맵에서 TPS 저하 0 확인
- verify: 수치 표로 보고(전/후 ms), "가볍습니다" 같은 무근거 표현 금지

## M8. 마감
- [ ] Dubs Bad Hygiene 물 연동(선택, 미설치 시 자체 자원)
- [ ] 번역 키 전수 정리(한/영), 누락 키 0
- [ ] 모드 제거 후 세이브 로드 오류 0
- [ ] Steam Workshop 업로드용 About.xml 정리(`modVersion`, 설명, 미리보기)

---

## 미해결 결정 (사용자 확인 필요)
- [ ] 밸런스: 하루 80% / 임계 70% / 내구도 −2%p — PLAN §4.2·§4.6 초기값 승인 여부
- [ ] DDD 계층 4개(Domain/Application/Infrastructure/Presentation) 유지 vs Domain+Infrastructure 2계층으로 축소
- [ ] 자동 갈아입기 우선순위: 중간(식사·수면 아래) vs 높음
- [ ] 1.7 대응 시점(게임이 1.6.4871 인 동안은 미대응)
