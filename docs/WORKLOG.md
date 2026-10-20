# Purrcelain Panic — 에셋 제작 작업 기록

게임 컨셉: 식탁 뒤에 기대어 선 이족보행 카툰 고양이가 손으로 도자기를 밀어 떨어뜨려 깨는 게임.
하체는 식탁에 가려지므로 걷기 애니메이션은 사용하지 않는다.

## 0. VARCO 3D 리소스 생성 (2026-10-15)

### 컨셉 이미지 프롬프트 (GenerateImage, nano-banana-2, 1:1, 각 2장)

| 에셋 | 프롬프트 |
|---|---|
| 고양이 | Stylized cartoon 3D game character, a cute chubby bipedal orange tabby cat standing upright like a human, full body, T-pose with arms straight out to the sides, legs apart, feet visible, big head, large expressive mischievous eyes, short arms with round paws, smooth clean shapes, simple flat colors, toy-like vinyl figure look, front view, plain white background, single character, no props, game asset concept |
| 식탁 | Stylized cartoon 3D game prop, a simple rectangular wooden dining table with four straight legs, chunky rounded edges, plain flat tabletop with nothing on it, warm medium brown wood with soft simple colors, clean low-detail toy-like shapes, three-quarter front view, full object visible, plain white background, single object, game asset concept |
| 도자기 | Stylized cartoon 3D game prop, a single porcelain flower vase, rounded belly with narrow neck and flared lip, bright white glossy ceramic with a simple light blue painted band pattern, closed simple form, clean smooth toy-like shapes, front view, full object visible, plain white background, single object, game asset concept |

공통 스타일 키워드(톤 통일 목적): `stylized cartoon`, `smooth clean shapes`, `toy-like`, `plain white background`.

결과 이미지: `Concept/` 폴더 (cat_A/B, table_A/B, vase_A/B)

### 선택한 컨셉

| 에셋 | 선택 | 이유 |
|---|---|---|
| 고양이 | `cat_B` | 깔끔한 T-pose(자동 리깅에 유리), 장난꾸러기 표정이 게임 컨셉에 맞음, 장신구 없음 |
| 식탁 | `table_A` | 장식 없는 단순한 박스형 → 단순 충돌 메시에 유리 |
| 도자기 | `vase_A` | 목·받침이 뚜렷한 실루엣, 밝은 흰색+하늘색으로 파괴 대상 식별 쉬움 |

### 3D 생성 설정 (Generate3D)

| 에셋 | polygonCount | topology | tPose | texture | 후처리 |
|---|---|---|---|---|---|
| 고양이 | 50,000 | tri | 1 | 2048, 비PBR | Rig (humanoid) |
| 식탁 | 20,000 | tri | 0 | 2048, 비PBR | — |
| 도자기 | 20,000 | tri | 0 | 2048, 비PBR | — |

생성 폴리곤 수를 넉넉히 두고, Blender에서 Decimate로 줄여 정리 전·후 비교 자료로 사용한다.

생성 결과(원본, 수정 없이 보존): `Source/cat_raw.glb`, `Source/cat_rigged.glb`(자동 리깅, 본 23개), `Source/table_raw.glb`, `Source/vase_raw.glb`
원본 미리보기: `Captures/raw/`

---

## Blender 정리 파이프라인 (Claude가 작성·실행한 bpy 스크립트)

Blender MCP가 연결되어 있지 않아 Claude가 `bpy` 스크립트를 작성하고 Blender 5.2를 백그라운드로 실행했다.
전체 재실행: `bash Blender/run_pipeline.sh` (항상 원본 GLB에서 다시 만든다)

| 단계 | 스크립트 | 대응 요청문 |
|---|---|---|
| 00 | `Blender/scripts/00_import.py` | 원본 불러오기, `SOURCE_Backup` 컬렉션에 복사본 보존, 불필요한 `Icosphere` 제거 |
| 01 | `Blender/scripts/01_cat.py` | 요청문 1 — 고양이 정리와 애니메이션 |
| 02 | `Blender/scripts/02_vase.py` | 요청문 2 — 도자기 파편 제작 |
| 03 | `Blender/scripts/03_table_style.py` | 요청문 3 — 식탁 정리와 스타일 통일 |
| 04 | `Blender/scripts/04_export.py` | 요청문 4 — 최종 검수와 FBX 출력 |

수치 원본: `docs/stats.json` (스크립트가 자동 기록)

### 검사 방법
- **구멍/Non-Manifold**: 열린 경계 엣지, 2면 초과 공유 엣지 수
- **뒤집힌 노멀**: 노멀 일관성 재계산 시 방향이 바뀌는 면 수
- **떨어진 조각**: 연결 요소(island) 수
- VARCO GLB는 UV 심마다 정점이 분리되어 있어(고양이 8,244 / 식탁 4,636 / 도자기 2,260 엣지) 그대로 검사하면 전부 '구멍'으로 잡힌다 → Merge by Distance 후 검사했다.

### 요청문 1 결과 — 고양이

| 항목 | 결과 |
|---|---|
| 이름 | 메시 `CAT_Body`, 아마추어 `CAT_Rig` |
| 높이 | 0.842 → **1.15 m** (×1.366) |
| 원점 | 양발 중앙 바닥 (0,0,0) |
| 정면 | Blender −Y → Unity +Z |
| Scale | `CAT_Body`, `CAT_Rig` 모두 (1,1,1) |
| 폴리곤 | 50,000 → **20,000 tris** (Decimate 0.4) |
| 정리 | UV 심 정점 4,268개 병합, 불필요 오브젝트 `Icosphere` 제거 |
| 노멀/구멍 | 뒤집힌 면 0, 구멍 0, 조각 1개 |
| Weight | 가중치 없는 정점 0 / 5개 이상 본 영향 정점 387 → 4본 제한 + 정규화 |
| 어깨 높이 | 0.68 m. 팔 각도는 식탁 높이(0.62 m)에 맞춰 자동 보정(손목이 상판 위 5 cm) |

| Action | 프레임 | 내용 |
|---|---|---|
| `Idle_Lean` | 1–31 (30fps, 30구간) | 상판에 앞발을 올리고 기댄 자세, 16프레임에 들숨, 머리 기울임/돌림. 1 = 31 동일 포즈 + Cycles 모디파이어로 루프 |
| `Push_R` | 1–16 | 1 기본 → 4 오른팔 뒤로 당김 → 8 손바닥을 앞으로 뻗은 접촉 → 16 복귀. 4→8 구간 Linear로 빠르게. 1회 재생 |

Root 본은 모든 키에서 위치/회전 0으로 고정. 포즈 확인 렌더: `Captures/cat_pose_check/`

### 요청문 2 결과 — 도자기

| 항목 | 결과 |
|---|---|
| 크기 | 높이 **0.30 m**, 지름 0.19 m, 원점 바닥 중앙 |
| `Vase_Intact` | 20,000 → **6,000 tris**, 뒤집힌 면 0 / 구멍 0 / Non-Manifold 0 |
| 파편 방식 | 10개 시드(층별 링 배치) 보로노이 셀 × Boolean(Exact) Intersect → 각 파편이 닫힌 메시 |
| 작은 조각 처리 | 3% 미만 조각이 나오면 해당 시드를 빼고 다시 자름 → 시드 1개 제거, **9조각** |
| 단면 색 | 단면 UV를 텍스처의 흰 도자기 영역(0.98)으로 지정 → 같은 머티리얼로 흰 단면 표현 |
| 계층 | `Vase_Broken_Root`(Empty, 원본과 같은 위치) 아래 `Vase_Shard_01`~`09` |

| 파편 | tris | 부피 % | 최소 치수 cm | 구멍 | Non-Manifold | 뒤집힌 면 |
|---|---|---|---|---|---|---|
| Vase_Shard_01 | 844 | 8.6 | 9.0 | 0 | 0 | 0 |
| Vase_Shard_02 | 882 | 8.8 | 9.5 | 0 | 0 | 0 |
| Vase_Shard_03 | 862 | 8.8 | 9.5 | 0 | 0 | 0 |
| Vase_Shard_04 | 622 | 15.5 | 9.9 | 0 | 0 | 0 |
| Vase_Shard_05 | 736 | 18.3 | 11.0 | 0 | 0 | 0 |
| Vase_Shard_06 | 686 | 17.7 | 9.6 | 0 | 0 | 0 |
| Vase_Shard_07 | 746 | 9.9 | 9.0 | 0 | 0 | 0 |
| Vase_Shard_08 | 864 | 7.8 | 10.2 | 0 | 0 | 0 |
| Vase_Shard_09 | 2,258 | 4.7 | 10.2 | 0 | 0 | 0 |

각 파편 원점 = 부피 중심, Scale/Rotation 적용 완료. 모든 파편 머티리얼 `M_Vase` 1슬롯.

시행착오 기록:
1. 첫 실행에서 6조각만 생성 → 원점 이동 후 바운딩 박스가 갱신되지 않아 시드가 0.15 m 아래에 배치됨 → `view_layer.update()` 추가로 해결
2. 작은 조각을 이웃과 Join하자 맞닿은 단면이 겹쳐 Non-Manifold 125개 발생 → Join 대신 "시드 제거 후 재절단" 방식으로 변경해 0개

### 요청문 3 결과 — 식탁과 스타일 통일

| 항목 | 결과 |
|---|---|
| 이름 | `TABLE_Main` |
| 크기 | 1.24 × 0.87 × 0.72 m → 요청문 5로 **1.80 × 0.60 × 0.62 m** |
| 원점/정면 | 바닥 중앙 / 긴 면이 −Y(Unity +Z) |
| 폴리곤 | 20,000 → **5,000 tris** (Decimate 0.25), 40° 이상 각진 엣지는 Sharp |
| 노멀/구멍/조각 | 0 / 0 / 1 (내부 메시나 떠 있는 조각 없음) |

머티리얼 (색 보정은 텍스처 픽셀에 직접 적용 → FBX/Unity에서도 유지):

| 에셋 | 머티리얼 | 텍스처 | 채도 | 명도 | Roughness | 의도 |
|---|---|---|---|---|---|---|
| 고양이 | material_0 → `M_Cat` | `T_Cat_BaseColor.png` | ×1.20 | ×1.08 | 0.65 | 가장 눈에 띄게 |
| 식탁 | material_0.001 → `M_Table` | `T_Table_BaseColor.png` | ×0.80 | 평균 0.50으로 | 0.65 | 중간 명도로 배경화 |
| 도자기(+파편) | material_0.002 → `M_Vase` | `T_Vase_BaseColor.png` | ×1.10 | ×0.88+0.14 | 0.35 | 밝은 색, 유약 광택 |

공통: Metallic 0, Specular 0.5, 거칠기 0.65 기준(도자기만 광택 표현을 위해 0.35).
배치 확인 렌더: `Captures/after/layout_idle.png`

### 요청문 4 결과 — FBX 출력

출력 설정: Selected Objects, Forward −Z, Up Y, Apply Scalings = FBX All, Apply Unit, **Apply Transform(bake_space_transform)**, Leaf Bones 끔, Deform 본만, 텍스처 Copy

| 파일 | 오브젝트 계층 | 크기 (m) | tris | 머티리얼 | 애니메이션 |
|---|---|---|---|---|---|
| `Export/Cat_Player.fbx` | `CAT_Rig` > `CAT_Body` | 1.37 × 0.70 × 1.15 (T-pose) | 20,000 | M_Cat | `Idle_Lean`, `Push_R` (Bake, 매 프레임) |
| `Export/Table_Main.fbx` | `TABLE_Main` | 1.80 × 0.60 × 0.62 | 5,000 | M_Table | 없음 |
| `Export/Vase_Set.fbx` | `Vase_Intact`, `Vase_Broken_Root` > `Vase_Shard_01`~`09` | 0.19 × 0.19 × 0.30 | 6,000 + 8,500 (파편 합) | M_Vase | 없음 |

재임포트 검증 (FBX를 다시 Blender로 불러와 비교):
- 세 파일 모두 크기와 바닥 높이(z=0) 일치, 빈 머티리얼 슬롯 0
- 고양이 오른손 위치 오차: `Idle_Lean` 16프레임 0.0 mm, `Push_R` 8프레임 0.0 mm
- 클립 이름: 처음에는 `CAT_Rig|Idle_Lean`으로 출력됨 → NLA 스트립 이름으로 출력하도록 바꿔 `Idle_Lean`, `Push_R`로 정리

---

## 과정 이미지 (평가 항목별 근거)

`SOURCE_Backup`(정리 전 원본 복사본)과 작업본(정리 후)을 같은 카메라·조명으로 렌더해 비교했다.
생성 스크립트: `Blender/scripts/render_process.py` → `Captures/process/`

| 평가 항목 | 이미지 | 보여주는 것 |
|---|---|---|
| 게임 규격 정리 | `P6_크기_원점.png` | 1 m 막대 기준 같은 축척 정면. 원본 고양이 0.84 m → 1.15 m, 식탁 0.72 m, 도자기 0.30 m, 빨간 점 = 원점(바닥 중앙) |
| 최적화 | `P1_최적화_와이어프레임_전후.png` | 와이어프레임 밀도 비교. 50,000→20,000 / 20,000→5,000 / 20,000→6,000 tris |
| 게임용 편집·검수 (구멍) | `P2_열린엣지_병합_전후.png` | 원본의 열린 엣지(빨강, UV 심 분리) 8,244 / 4,636 / 2,260개 → Merge by Distance 후 0개 |
| 게임용 편집·검수 (노멀) | `P3_노멀방향_검사.png` | Face Orientation 검사. 모두 파랑(바깥), 뒤집힌 면 0 |
| 게임용 편집·검수 (파츠 분리) | `P5_도자기_파츠분리.png` | ① 원본 → ② 보로노이 절단 → ③ 9조각 분리 → ④ 같은 머티리얼 + 흰 단면 |
| 스타일 통일 | `P4_머티리얼_톤통일_전후.png` | 원본 머티리얼(Roughness 1.0) vs 톤 통일 후(M_Cat/M_Table/M_Vase) |
| 애니메이션 정리 | `P7_Push_R_키포즈.png`, `P8_Idle_Lean_키포즈.png` | 키프레임별 포즈 (1/4/8/16F, 1/8/16/24F) |

수정 기록: 처음 만든 `SOURCE_Backup`은 메시만 복사하고 머티리얼은 작업본과 공유하고 있어서, 색 보정이 백업에도 적용되고 있었다 → `00_import.py`에서 머티리얼도 `SRC_` 복사본으로 분리하도록 고치고 전체 파이프라인을 다시 실행했다. (P4의 "원본 머티리얼"은 이 수정 후 렌더)

---

## 요청문 5 — 식탁 넓히고 낮추기 (추가 요청)

> 책상 좀 넓히고 높이 좀 낮출 수 있나? 이 게임은 도자기가 책상에 점점 많이 생성되고 그걸 다 깨는 재미인데 책상이 너무 작네. 고양이 팔이 안닿으려나

| 항목 | 처리 |
|---|---|
| 크기 | 1.24 × 0.87 × 0.72 m → **1.80 × 0.60 × 0.62 m** (`pp_common.TABLE_SIZE` 한 곳에서 관리) |
| 변형 방법 | 균일 스케일을 하면 다리가 굵어지므로, 다리·모서리는 그대로 두고 상판 가운데 구간만 늘림. 높이는 다리 중간 구간만 압축 (`03_table_style.resize_table`) |
| 깊이를 줄인 이유 | 고양이 팔이 닿는 거리 확보 |
| 고양이 팔 | 식탁이 낮아지면 앞발이 떠 있으므로, 기본 자세에서 손목이 상판 위 5 cm에 오도록 팔 각도를 이분 탐색으로 자동 보정 (+13°) |
| 접촉 자세 | Push_R 8F에서 손끝이 상판보다 2 cm 아래로 파고들어 → 팔을 6° 올려 상판 위 4 cm로 수정 |

닿는 거리 측정 (고양이 원점 기준):
- 배 앞면 0.27 m, Push_R 접촉 시 손끝 0.53 m → 고양이가 식탁 뒷면에 붙어 서면 **상판 뒤쪽 약 0.25 m(깊이의 약 40%)까지** 닿는다
- 손이 닿는 좌우 폭은 고양이 중심 ±0.3 m 정도 → 1.8 m 식탁 전체를 커버하려면 Unity에서 고양이를 좌우로 이동시켜야 한다 (하체가 가려지므로 걷기 애니메이션 없이 평행 이동 가능)
- 도자기는 상판 뒤쪽 0.25 m 안에 생성하거나, 밀었을 때 앞으로 미끄러져 떨어지게 물리 설정

확인 렌더: `Captures/after/layout_idle.png`, `Captures/after/layout_push8.png`

---

## Unity 결과 검증

Unity 6000.6 (URP) 프로젝트 = 이 저장소 루트.

| 단계 | 내용 |
|---|---|
| 정리 전 FBX | `Blender/scripts/05_export_before.py`로 `SOURCE_Backup`을 같은 FBX 설정으로 출력 → `Assets/Art/Before/` (원본 텍스처는 .blend 안에 packed 상태라 PNG로 꺼내서 연결) |
| 정리 후 FBX | `Assets/Art/Cat`, `Assets/Art/Table`, `Assets/Art/Vase` |
| 고양이 임포트 | Rig = Humanoid (Avatar valid), `Idle_Lean` Loop Time ON, `Push_R` Loop Time OFF, Root 회전·높이·XZ Bake Into Pose |
| 소품 임포트 | Import Animation OFF |
| 머티리얼 | `Assets/Art/Materials/`로 추출, URP Lit. Smoothness 고양이·식탁 0.35 / 도자기 0.65 (Blender Roughness는 FBX로 넘어오지 않음) |
| 비교 씬 | `Assets/Scenes/Compare_BeforeAfter.unity` |
| 캡처 | `Captures/unity/Unity_Compare_BeforeAfter.png` |

Unity에서 측정한 값 (정점 기준):

| | 정리 전 | 정리 후 |
|---|---|---|
| 고양이 | 1.00 × 0.84 × 0.51 m, 50,000 tris | 1.37 × 1.15 × 0.70 m(T-pose), 20,000 tris, 원점 발밑 |
| 식탁 | 1.00 × 0.58 × 0.70 m, 20,000 tris, 원점 중앙(바닥에 반쯤 묻힘) | 1.80 × 0.62 × 0.60 m, 5,000 tris, 원점 바닥 |
| 도자기 | 0.64 × 1.00 × 0.64 m(1 m짜리 도자기), 20,000 tris, 원점 중앙 | 0.19 × 0.30 × 0.19 m, 6,000 + 파편 9개, 원점 바닥 |

세 정리 후 에셋 모두 Unity에서 Rotation (0,0,0), Scale (1,1,1).
참고: 정리 전 도자기의 어두운 줄무늬는 VARCO 원본 머티리얼의 두 번째 텍스처가 Unity에서 다르게 해석된 것으로, 정리 후 머티리얼(`M_Vase`)에는 없다.

---

## 요청문 6 — 왼손 밀기 Push_L (추가 요청)

> 왼손 PUSH_1도 만들어줘. PUSH_R이랑 동일하게, 손만 바꿔서 만들어주면 돼.

| 항목 | 처리 |
|---|---|
| 이름 | `PUSH_1`은 `Push_R`과 짝이 맞도록 `Push_L`로 정리 |
| 만드는 방법 | `Push_R` 키 포즈(`PUSH_R_KEYS`)를 좌우 반전: Right↔Left 본 이름 교체 + 좌우 대칭이므로 Y·Z축 회전 부호 반전 (`01_cat.mirror_keys`) |
| 타이밍 | `Push_R`과 동일: 16프레임 1회, 1 기본 → 4 준비 → 8 접촉 → 16 복귀, 4→8 Linear |
| 거울 검증 | 각 키에서 왼손 끝과 오른손 끝을 X축 대칭으로 비교 → 1·4·8·16F 모두 오차 0.0 mm |
| FBX | `Cat_Player.fbx`에 클립 3개(Idle_Lean, Push_R, Push_L), 다시 불러와 왼손 위치 오차 0 mm |
| Unity | Humanoid Avatar 정상, `Push_L` Loop Time OFF, 0.50초 |

키포즈 이미지: `Captures/process/P9_Push_L_키포즈.png`

---

## 요청문 7 — 도자기 변형체 2종 (추가 요청)

> 그 도자기 같은 것만 나오면 너무 지루하니까, 색깔과 모양이 조금씩 다른 변형체를 2개만 추가해줘. 조금 늘리거나 누르는 식으로.

| 항목 | Vase_Tall (늘림) | Vase_Squat (누름) |
|---|---|---|
| 변형 (X·Y·Z) | 0.90 · 0.90 · 1.22 | 1.16 · 1.16 · 0.78 |
| 크기 | 0.17 × 0.17 × 0.37 m | 0.22 × 0.22 × 0.23 m |
| 색 | 청자 느낌: 민트빛 유약 + 초록 무늬 | 분홍 느낌: 크림빛 유약 + 장미색 무늬 |
| 머티리얼 / 텍스처 | `M_Vase_Tall` / `T_Vase_Tall_BaseColor.png` | `M_Vase_Squat` / `T_Vase_Squat_BaseColor.png` |
| 구조 | `Vase_Tall_Intact`, `Vase_Tall_Broken_Root` > `Vase_Tall_Shard_01~09` | `Vase_Squat_Intact`, `Vase_Squat_Broken_Root` > `Vase_Squat_Shard_01~09` |
| tris | 6,000 + 파편 8,500 | 6,000 + 파편 8,500 |
| 검사 | 원본·파편 모두 구멍 0 · Non-Manifold 0 · 뒤집힌 면 0 | 같음 |
| FBX | `Export/Vase_Tall_Set.fbx` | `Export/Vase_Squat_Set.fbx` |

처리 방법 (`Blender/scripts/03b_vase_variants.py`):
- 원본 도자기와 파편 9조각에 **같은 비율 변형**을 적용 → 파편이 변형된 도자기와 정확히 맞물리고, 깨지는 구조를 그대로 유지
- 원점이 바닥 중앙이라 변형 후에도 바닥은 z=0, 파편 원점은 변형 후 부피 중심으로 다시 지정
- 색은 원본 보정 텍스처에서 무늬(채도 있는 픽셀)는 색상만 옮기고, 흰 유약(채도 낮은 픽셀)은 살짝 물들임 → 원본과 같은 명암·광택(Roughness 0.35)을 유지하면서 색만 다르게
- 고양이 주황과 겹치지 않도록 초록·분홍 계열 선택

Unity: `Assets/Art/VaseVariants/`, 머티리얼 추출 후 Smoothness 0.65, Rotation 0 · Scale 1, 파편 9개 확인.
확인 렌더: `Captures/process/P10_도자기_변형체.png`

---

## 요청문 8 — 식탁 깊이 줄이기 (기획 검토 반영)

> 손 도달 범위는 책상을 좀 줄이고

| 항목 | 처리 |
|---|---|
| 문제 | 앞발이 상판 뒤쪽 0.25 m까지만 닿는데 깊이가 0.60 m → 덜 밀려 안쪽에 멈춘 도자기는 다시 칠 수 없어 식탁이 막힘 |
| 크기 | 1.80 × 0.60 × 0.62 m → **1.80 × 0.45 × 0.62 m** (`pp_common.TABLE_SIZE`) |
| 근거 | 도자기 지름 0.19~0.22 m → 깊이 방향 2줄. 먼 줄 도자기 중심(앞 가장자리에서 약 0.34 m)도 손끝 0.25 m + 판정 반경 0.08 m + 도자기 반지름 안에 들어옴 |
| 고양이 위치 | 렌더 스크립트의 고양이 위치를 `CAT_STAND_Y = 깊이/2 + 0.25`로 바꿔 식탁 크기를 따라가게 함 |
| 재출력 | `run_pipeline.sh` 전체 재실행 → 식탁만 바뀜(고양이·도자기 수치 동일, 해당 FBX는 기존 파일 유지). `Table_Main.fbx`를 `Assets/Art/Table/`에 반영 |

확인 렌더: `Captures/after/layout_push8_d45.png`

---

## 요청문 9 — 앞발 휘두르기 Swipe_R / Swipe_L

> Swipe 애니메이션 지금 만들어줘 이거부터 가자.

기획서 8장: 손바닥으로 정면을 미는 Push 대신, 앞발을 몸 안쪽으로 당겼다가 바깥·전방으로 곡선을 그리며 쓸어낸다.

| 항목 | 처리 |
|---|---|
| 만드는 방법 | `01_cat.py`의 `SWIPE_R_KEYS` 키 포즈 3개 + `mirror_keys`로 `Swipe_L` 생성 (Push와 같은 방식) |
| 타이밍 | 16프레임(0.53초) 1회: 1 기본 → 4 준비 → 7 타격 → 9 마무리 → 16 복귀. 4→7→9 Linear로 빠르게 |
| 4F 준비 | 앞발을 가슴 앞 반대쪽(오른손이면 왼쪽)으로 당기고 상체를 그쪽으로 10° 비틂 |
| 7F 타격 | 앞-바깥쪽으로 가장 멀리 뻗음: 손끝이 상판 안쪽 **0.28 m** (Push 0.25 m보다 멀리) |
| 9F 마무리 | 바깥으로 쓸고 나감, 상체가 반대로 풀림 (손끝 좌우 0.55 m) |
| 타격 구간 | 5~9F (`SWIPE_HIT_FRAMES`) — Unity에서 이 구간만 손 궤적 Sphere Cast |
| 상판 관통 검사 | 프레임마다 손끝 주변 8 cm 안의 앞발 메시 최저점을 측정. 타격 구간 5~9F 모두 상판 위 0.0~5.2 cm (7F 첫 시도에서 3.4 cm 파고들어 → 팔을 5° 올려 해결) |
| 거울 검증 | 1·4·7·9·16F 왼손·오른손 끝 X축 대칭 오차 0.0 mm |
| FBX | `Cat_Player.fbx` 클립 5개(Idle_Lean, Push_R, Push_L, Swipe_R, Swipe_L), 다시 불러와 손 위치 오차 0 mm |
| Unity | Humanoid Avatar 정상, `Swipe_R`·`Swipe_L` Loop Time OFF, 0~15프레임 |

오른손 손끝 궤적 (고양이 원점 기준, 좌우 x는 + = 고양이 왼쪽):

| 프레임 | 1 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|
| 좌우 x (m) | −0.17 | +0.16 | +0.10 | −0.02 | −0.16 | −0.38 | −0.55 |
| 상판 안쪽 (m) | 0.20 | −0.04 | 0.11 | 0.22 | 0.28 | 0.20 | 0.02 |
| 앞발 최저점 − 상판 (cm) | −1.8 (기댄 자세) | 2.9 | 4.3 | 3.5 | 0.0 | 2.4 | 5.2 |

키포즈 이미지: `Captures/process/P11_Swipe_R_키포즈.png`, `Captures/process/P12_Swipe_L_키포즈.png`

---

## 1단계 — 카메라와 그레이박스 (게임 구현 시작)

> 응 커밋하고 1단계 가자

| 항목 | 처리 |
|---|---|
| 씬 | `Assets/Scenes/Game.unity` (빌드 목록 첫 번째). 회색 바닥, 실제 식탁·고양이 에셋 사용 |
| 좌표 | 고양이는 +Z를 바라봄 → 먼 쪽(파손 바닥) = +Z. 식탁 중심 원점, 고양이 z = −(0.225 + 0.25) |
| 고양이 | `Assets/Animation/Cat_Player.controller` (기본 Idle_Lean, Swipe_L·Swipe_R 상태), Animator Update Mode = Fixed(AnimatePhysics), Root Motion 끔 |
| 도자기 미리보기 | 2줄 × 7열 SpawnPoint 위치 중 8곳에 세 종류 배치 (`Vases_Preview`, 생성 시스템은 4단계에서 교체) |
| 카메라 | `Assets/Scripts/Camera/CameraFollow.cs`: X축 SmoothDamp 추적, 흔들림 함수, 편집 모드에서도 위치 갱신 |

카메라 구도 결정:
- 기획서 구도(머리 뒤, 높이 1.55 m, 45°)로 찍어 보니 머리가 화면 하단 절반을 덮고 앞발과 가운데 앞줄 도자기를 가림
- 원인: 머리 폭 0.51 m, 머리 앞끝이 z −0.09 m로 식탁 앞 가장자리(−0.225 m)를 넘어오고, Swipe 타격 순간 손목(z −0.075 m)이 바로 그 밑에 있음
- 45° / 55° / 78° 세 구도 비교 (`Captures/unity/Camera_Option1~3_*.png`) → **머리 바로 위 75°** 선택: 높이 2.6 m, 고양이 원점보다 0.15 m 앞, FOV 50°
- 결과: 머리는 화면 아래 중앙(정수리와 귀), 앞발 휘두르기와 두 줄 도자기 모두 보임, 식탁 뒤 바닥 1.63 m까지 보임

확인 캡처: `Captures/unity/Greybox_Idle_Lean_F1.png`, `Greybox_Swipe_R_F7.png`, `Greybox_Swipe_L_F7.png`

식탁 폭 조정 (추가 요청: "식탁 크기만 좀 조절하자. 너무 작네"):
- 75° 카메라에서 1.8 m 식탁이 화면 가로의 44%뿐 → 폭만 **2.70 m**로 늘림 (깊이 0.45 m는 앞발 도달 범위 때문에 유지). 화면 가로 약 80%
- `pp_common.TABLE_SIZE = (2.70, 0.45, 0.62)` → 파이프라인 재실행, 식탁만 바뀜
- SpawnPoint 격자 11열 × 2줄(22개), 스테이지별 사용 열 5 / 7 / 11 (기획서 부록 B-1, B-8)
- 캡처 참고: Unity 에디터가 백그라운드에 있으면 GPU Resident Drawer와 스키닝이 갱신되지 않아 캡처가 이전 상태로 나온다. 캡처할 때만 GPU Resident Drawer를 끄고, 고양이 포즈는 메시로 구워(BakeMesh) 찍음. 그림자 맵은 여전히 이전 배치로 남아 있음 (실행 중에는 문제없음)

식탁 깊이 조정 (추가 요청: "깊이도 좀 깊어도 될 것 같아. 어차피 도자기끼리 부딪히면 밀려나잖아"):
- 깊이 0.45 m → **0.80 m** (`TABLE_SIZE = (2.70, 0.80, 0.62)`), SpawnPoint 3줄(0.11 / 0.33 / 0.55 m) × 11열 = 33개
- 앞발이 직접 닿는 건 앞 2줄(가장자리에서 0.43 m까지). 뒷줄 막힘 방지: 뒷줄은 앞뒤 배열일 때만 생성. 멈춘 뒷줄 도자기를 밀어주는 구조 규칙은 미정 (3단계 이후 다시 정함)
- Domino Display Lv 4+의 앞뒤 3개 직선 배열을 본문대로 되살림
- 고양이 z = −(0.40 + 0.25), 카메라 72°로 낮춤 → 식탁 뒤 바닥 1.52 m까지 보임

---

## 2단계 — 양손 Swipe

> 구조 규칙은 나중에 다시 잡아. 커밋하고 2단계 가자

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Core/GameTuning.cs` + `Assets/Data/GameTuning.asset` | 이동·Swipe 수치 한 곳 관리 (기획서 부록 B-1, B-3) |
| `Assets/Scripts/Cat/CatController.cs` | 새 Input System 액션(A/D·←/→, 마우스 왼쪽/오른쪽), X축 직접 이동(가속 0.08초, 충전 중 ×0.6, 범위 ±1.3 m) |
| `Assets/Scripts/Cat/PawSwipe.cs` | 손별 상태(준비 → 충전 → Swipe), 충전 중 준비 자세(4F) 유지, 놓으면 쿨다운 0.6초, 타격 구간(5~9F) 손끝 경로 Sphere Cast, `AddForceAtPosition`(Impulse 1.5~4.5), `VaseHit` 이벤트 |
| Animator | Base(Idle_Lean) + `PawL`·`PawR` 레이어(아바타 마스크: 팔 + 상체). 레이어 시간은 PawSwipe가 `Play(정규화 시간)`으로 직접 넘김 → 양손이 서로 끊지 않고 독립 |
| 도자기 프리팹 | `Assets/Prefabs/Vases/Vase_Normal / Fragile / Collector` (질량 1.0 / 0.75 / 1.25, Continuous Dynamic). 충돌체는 높이 6단 × 10각 저폴리 convex 메시 (원본 6,000 tris는 convex 256면 제한 초과) |
| 물리 | 레이어 Table·Vase·Shard·Floor, 물리 재질 `PM_Vase`·`PM_Table` (마찰 0.4/0.3, 탄성 0), 식탁 Mesh Collider |

검증 — 편집 모드에서 `Physics.simulationMode = Script`로 0.02초씩 직접 진행 (씬 사본에서 실행 후 삭제):
- 오른손 1초 충전 → 가운데 줄 도자기 Impulse 4.5, 식탁 먼 가장자리 너머 0.94 m에 낙하. 그 도자기에 부딪힌 뒷줄 도자기도 낙하 (물리 충돌만으로)
- 왼손 짧은 탭 → 앞줄 도자기 Impulse 1.5, 식탁 위에서 0.3 m 밀림
- 충전 중 앞발이 준비 자세에 멈춰 있는 것 확인

문제와 해결:
- 처음에 타격 0회: 판정 중심을 손목에서 0.06 m만 옮겨 손끝까지 닿지 않음 → 0.10 m
- 손 본이 움직이지 않음: Animator Culling Mode가 `CullUpdateTransforms`라 보이지 않으면 본 갱신을 건너뜀 → `AlwaysAnimate`
- Unity 6에서 물리 재질 확장자는 `.physicMaterial`

남은 점: 손 이동 방향 60% + 전방 40% 규칙대로면, 휘두르기 후반에 맞은 바깥쪽 도자기는 옆으로 밀린다 (왼손 탭: 좌우 성분이 전방의 5배). 플레이해 보고 손 방향 비중을 조정할지 정한다.

---

## 3단계 — 도자기 물리와 파괴

> 커밋하고 3단계 가자 나쁘지 않네

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Vase/BreakableVase.cs` | Swipe 번호·연쇄 단계·받은 속도 기록, 연쇄 힘 전달(Swipe당 한 번), 식탁 이탈 지점 기록, 바닥에 처음 닿으면 파괴 + `Broken` 이벤트(충돌 속도·비행 거리·연쇄 단계) |
| `Assets/Scripts/Vase/TableSurface.cs` | 상판 범위. 도자기 무게 중심이 처음 벗어난 XZ 위치 = 식탁 이탈 지점 |
| `Assets/Scripts/Vase/ShardSet.cs`, `ShardPool.cs` | 9조각 파편 세트를 종류별 12개씩 미리 만들어 돌려 씀. 원래 모양대로 맞춘 뒤 바깥·위로 흩뿌리고 4초 뒤 0.5초 동안 줄어들며 반환 |
| 프리팹 | `Assets/Prefabs/Shards/Shards_Normal / Fragile / Collector` (조각마다 Box Collider + Rigidbody, 질량 = 도자기 ÷ 9), 도자기 프리팹 3종에 `BreakableVase` |
| 물리 | Shard 레이어는 Floor·Table과만 충돌 |
| 연결 | `PawSwipe`가 맞힌 도자기에 `OnPawHit`로 Swipe 정보 전달 |

연쇄 규칙 (기획서 B-4): 친 쪽 도자기가 0.5 m/s 이상이고 같은 Swipe에서 아직 전달하지 않았으면, 맞은 도자기 속도 = 충돌 직전 속도 × 전달률 0.6 (방향: 두 중심을 잇는 방향 70% + 친 쪽 진행 방향 30%), 친 쪽은 속도 ×0.4. 같은 Swipe 번호를 이미 가진 도자기에는 전달하지 않아 무한 연쇄 방지. 처음부터 붙어 있던 도자기를 위해 `OnCollisionStay`에서도 확인.

검증 — Unity가 백그라운드에서는 Play 모드에서도 프레임이 멈춰 있어, Play 모드에 들어간 뒤 `Physics.simulationMode = Script`로 0.02초씩 직접 진행 (충돌 콜백은 실제와 같이 호출됨):
- 오른손 정면 x = +0.24 m 열에 앞뒤 3개 배치, 오른손 1초 충전
- 앞발이 앞줄·가운데 줄을 직접 맞힘 (각 4.5 m/s), 뒷줄은 연쇄 1단계로 2.58 m/s (= 4.3 × 0.6)
- 세 개 모두 바닥에서 파괴: 충돌 속도 3.55 / 4.39 / 3.98 m/s, 비행 거리 0.28 / 0.84 / 0.76 m
- 2초 뒤 파편 세트 3개(조각 27개)가 바닥에, 5초 뒤 모두 풀로 반환

능력 없는 초기 상태에서는 최대 충전이어도 ImpactQuality가 0.5 안팎이라 PURRFECT(0.70 이상)가 거의 안 나온다. 결정: 의도대로 둔다. Iron Paw 등 능력이 쌓이며 PURRFECT가 나오기 시작하는 것이 성장감이다.

---

## 4단계 — 생성 시스템

> 커밋하고 4단계 가자. 퍼펙트 안나오는건 그럴 수 있어. 지금 능력 아무것도 없는 초기 단계잖아

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Vase/VaseSpawner.cs` | 11열 × 3줄 격자. 패턴(단독·좌우 2개·앞뒤 2개·앞뒤 3개·군집 3개)을 가중치로 고르고, 모든 칸이 비어 있는 자리 중 무작위. 안 되면 단독, 그것도 안 되면 건너뜀. 깨진 도자기는 다시 꺼내 씀 |
| `Assets/Scripts/Core/StageSettings.cs` | 스테이지별 시작 시각·간격·열 수·패턴·종류 가중치 (기획서 B-8 값이 `GameTuning.stages` 기본값) |
| `Assets/Scripts/Core/StageDirector.cs` | 경과 시간으로 스테이지 전환 → 생성 규칙 교체, 고양이 이동 범위 = 쓰는 열 양 끝 + 0.1 m |

생성 규칙:
- 빈 칸 = 칸 중심 위 반경 0.12 m 안에 도자기가 없음 (`Physics.CheckSphere`, 플래그 없음)
- 뒷줄은 같은 열 바로 앞 칸도 함께 놓는 배열(앞뒤 2·3개, 군집)일 때만
- 앞발을 쓰는 중이면 고양이 중심 ±0.36 m 열에는 놓지 않음
- 능력 카드용 값 미리 열어 둠: `LineWeightBonus`·`Line3Weight`(Domino Display), `ExtraVaseChance`(Full Set), `PackedChance`(Packed Display)

검증 — Play 모드에서 0.02초씩 직접 진행:
- 아무것도 깨지 않고 180초: 60초·120초에 스테이지 전환(이동 범위 ±0.58 → ±0.82 → ±1.30 m). 식탁이 차면 생성이 멈춤 (스테이지 1은 앞 2줄 × 5열 = 10개에서 정지). 도자기끼리 0.15 m 안으로 겹친 경우 0, 앞 칸 없는 뒷줄 도자기 0, 저절로 떨어진 도자기 0
- 놓자마자 치우는 상태로 스테이지 3을 60초: 생성 50회(간격 1.20초), 93개, 종류 Normal 69% / Fragile 17% / Collector 14% (기준 60 / 25 / 15)

7단계 메모: 스테이지 전환·식탁 구간(열) 확장·Fragile/Collector 등장 확률은 이번에 데이터로 들어감. 남은 것은 Fragile·Collector 전용 머티리얼(밝은 머티리얼, 금색 장식)과 Collector 점수·경험치 보너스.

생성 문제 수정 (추가 요청: "도자기가 2개씩 겹쳐 나올때 서로 부딪혀서 넘어짐", "고양이 손위치에 생성되서 겹침 문제 발생"):
- 원인 1: 줄 간격 0.22 m < Collector 지름 0.223 m, 열 간격 0.24 m도 여유가 거의 없음 → 앞뒤·좌우로 붙여 놓으면 처음부터 겹쳐 밀어내다 넘어짐. 열·줄 간격 모두 **0.25 m** (줄 0.12 / 0.37 / 0.62 m, 열 −1.25 ~ +1.25 m)
- 원인 2: 앞발에는 충돌체가 없어 쉬는 앞발이 올라간 앞줄 칸이 빈 칸으로 판정됨 → `PawSwipe.DistanceToPawsXZ`(팔뚝~손끝 선분까지 거리)로 앞발 0.18 m 안의 칸은 생성 금지. 휘두르는 중에는 고양이 ±0.65 m 앞 2줄 금지 (휘두르는 폭 약 ±0.55 m)
- 검증 (Play 모드 직접 진행, 180초): 고양이를 10초마다 무작위 위치로 옮기고 1초마다 도자기 하나를 치우며 생성 171개 → 생성 순간 이웃과 최소 중심 거리 0.25 m, 저절로 넘어진 도자기 0. 휘두르기를 섞은 실행에서도 생성 순간 앞발과 최소 거리 0.18 m (겹침 0)
- 남은 점: 이미 놓인 도자기 쪽으로 고양이가 걸어가면 쉬는 앞발이 도자기와 겹쳐 보인다 (앞발 충돌체가 없음)

첫 줄 위치와 앞발 충돌체 (추가 요청: "1번으로 하는데, 아직도 도자기가 혼자 넘어지는 현상이 있네. 생성되는 첫줄을 조금 뒤로 밀까?"):
- 상판 높이 측정: 가장자리 3 cm만 둥글고 그 안쪽은 ±1 mm로 평평 → 모서리가 원인은 아님
- 줄 위치 0.12 / 0.37 / 0.62 → **0.19 / 0.44 / 0.69 m** (첫 줄을 쉬는 앞발 끝 약 0.2 m 바로 앞으로). 가운데 줄 0.44 m는 앞발 판정이 닿는 한계(약 0.47 m) 안
- 생성 칸 0.45 m 안에 0.15 m/s 이상으로 움직이는 도자기가 있으면 그 칸에는 생성하지 않음 (미끄러지던 도자기가 막 놓인 도자기를 치는 경우 방지)
- 앞발 충돌체(`PawBumpers`: 쉬는 앞발에 키네마틱 캡슐, 도자기 maxDepenetrationVelocity 1 m/s) 시험 → 실패. 2.2 m/s로 걸어가면 세 종류 모두 넘어지고, 0.8 m/s에서도 Fragile이 넘어짐. 휘두르기 없이 3분 걷기만 해도 25개 넘어지고 23개 낙하 → 씬에서 제거 (스크립트는 남겨 둠)

생성 직후 넘어짐과 앞발 겹침 (추가 답변: 넘어짐은 "생성 직후 바로", 앞발은 "부드럽게 밀기" → 결과 보고 후 "이동 중 앞발 당기기"):
- **넘어짐 원인 = 재사용 버그.** 깨진 도자기를 다시 꺼낼 때 비활성 상태에서 속도를 0으로 바꿨는데, 비활성 Rigidbody에는 반영되지 않아 깨질 때의 속도·회전을 가진 채 나타남. 재현: 아래로 4 m/s·회전 8 rad/s를 준 뒤 재사용 → 0.5초 뒤 141° 뒤집힘. 수정: 켠 뒤에 위치·속도 초기화 + 깨질 때도 속도 0으로 만든 뒤 비활성화. 이전 시뮬레이션은 멈춰 있던 도자기만 치워서 재현되지 않았음
- 부드럽게 밀기(`PawNudge`, 앞발과 겹친 도자기를 가속 제한으로 밀기) 시험 → 실패. 무게 중심을 2.5 m/s²로 밀면 정지 마찰(약 3.9 m/s²)을 못 넘겨 통과, 바닥 높이를 8 m/s²로 밀면 1.2 m/s 이상에서는 앞발이 도자기 중심을 지나치며 미는 방향이 뒤집혀 통과, 0.6 m/s에서는 고양이 쪽으로 밀려 일반·Fragile이 넘어짐 → 삭제
- **이동 중 앞발 당기기**: Animator에 팔만 덮는 `TuckL`·`TuckR` 레이어(Swipe 레이어 아래)를 추가하고 `Push_L/R` 4프레임(팔을 몸쪽으로 당긴 준비 자세)을 정지 재생. 좌우로 움직이는 중이거나 쉬는 자세의 앞발 자리에 도자기가 있으면 0.1초에 걸쳐 당김. 당긴 자세에서 손목은 상판 가장자리 4 cm 바깥
- 검증 (Play 모드 직접 진행): 첫 줄 도자기 세 종류를 2.2 m/s로 지나가다 오른쪽 앞발 자리 위에 멈춤 → 도자기 이동 0, 기울어짐 0, 멈춘 뒤에도 앞발이 당겨진 채 0.17 m 떨어져 있음. 180초 (1.5초 걷기·1.5초 서기 반복, 3초마다 휘두르기, 50개 파괴·재사용) → 생성 1초 안에 넘어짐 0, 맞지 않고 넘어짐 2 (다른 도자기에 밀림), 앞발과 도자기 중심이 0.12 m 안으로 겹친 표본 0.8% (블렌드 중)

---

## 5단계 — 등급과 점수

> 커밋하고 5단계 가자 지금 괜찮네

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Vase/ImpactEvaluator.cs` | ImpactQuality = SpeedScore × 0.65 + DistanceScore × 0.35 → CRACK(< 0.35) / SMASH(< 0.70) / PURRFECT. Fragile은 PURRFECT 경계 0.60 |
| `BreakableVase` | 깨질 때 등급을 정해 `VaseBreak.impact`에 담고, 파편 세기를 등급별 0.3 / 0.6 / 1.0으로 |
| `Assets/Scripts/Score/ScoreComboManager.cs` | 점수 = 등급 점수(100/160/250) × Collector 2배 × 콤보 배율. 콤보는 첫 파괴 = 1단계, 배율 1 + (단계 − 1) × 0.25 (최대 3배), 마지막 파괴 후 2초 지나면 0. 경험치 = 등급 1/2/3 + Collector 2 (콤보 배율 없음). 최고 콤보·PURRFECT 횟수 기록, Combo Keeper용 유지시간 보너스 |
| `Assets/Scripts/Effects/BreakEffects.cs` | 먼지 4 / 10 / 16개, 부스러기 0 / 12 / 30개 (Fragile ×1.5), 카메라 흔들림 SMASH 0.04 m · PURRFECT 0.08 m |
| `Assets/Scripts/UI/HudView.cs` | 점수, 콤보 단계·배율·남은 시간 막대, 깨진 자리 위에 등급 문구와 점수 (0.9초 동안 커졌다가 떠오르며 사라짐). 그레이박스라 내장 폰트로 코드에서 생성 |
| 씬 | `FX_Dust`·`FX_Crumbs` 입자(부드러운 원 텍스처 `T_SoftDot`, 부스러기는 바닥·식탁과 충돌), `Game`에 점수·연출, `HUD` |

검증 (Play 모드 직접 진행):
- 판정 함수: (3 m/s, 0 m) → 0.00 CRACK, (4.5, 0.75) → 0.50 SMASH, (5.5, 1.2) → 0.82 PURRFECT, (5, 1) Normal 0.67 SMASH / Fragile PURRFECT
- 가까운 도자기를 조준해 휘두르는 120초: 20개 파괴 (CRACK 18, SMASH 1, PURRFECT 1), 최고 콤보 5 (x2.00), 끝난 콤보 2·2·5·4. 모든 이벤트의 배율·점수가 공식과 일치, 점수 합계 = 2,660. PURRFECT 때 부스러기 30개

후속 작업 기록 (요청: "나중에 이거 점수 뜨는거 이펙트랑 폰트 추가할거니까 기록해놓고"): 기획서 부록 C에 점수 팝업 이펙트, HUD·팝업 폰트, 미정인 뒷줄 구조 규칙을 모아 둠.

---

## 6단계 — 경험치와 카드

> 커밋하고 6단계 가자. 나중에 이거 점수 뜨는거 이펙트랑 폰트 추가할거니까 기록해놓고

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/Ability/AbilityCatalog.cs` | 능력 9종 이름과 레벨별 설명 (모두 패시브) |
| `Assets/Scripts/Ability/ExperienceAbilityManager.cs` | 점수 이벤트의 경험치를 모아 레벨업 (요구량 4 + 3 × (레벨 − 1)). 레벨업하면 `Time.timeScale = 0`, 충전 취소, 카드 최대 3장. 한 번에 여러 레벨이 오르면 이어서 보여 줌. 슬롯 6개, 최대 Lv 5, 슬롯이 차면 보유 능력 강화만, 최대 레벨 제외, 고를 카드가 없으면 500점 |
| 능력 효과 연결 | Iron Paw → `PawSwipe.ImpulseMultiplier`, Domino Effect → `BreakableVase.ChainTransferBonus` (최대 95%), Quick Swipe → 쿨다운 배율(클립 재생 속도도 따라감), Wide Arc → Sphere Cast 반경 배율, Alternating Paws → 직전 Swipe가 반대 손·적중·1.5초 안이면 이번 Swipe 힘 추가, Combo Keeper → 콤보 유지시간, Domino Display·Full Set·Packed Display → 생성기 가중치·확률 |
| `Assets/Scripts/UI/CardSelectView.cs` | 카드 화면 (숫자 1·2·3, 숫자패드 포함). 장수가 적으면 가운데 정렬. 멈춘 동안 마우스 클릭은 앞발 입력으로 받지 않음 (`CatController`) |
| `HudView` | 레벨·경험치 막대, 보유 능력 목록 |

검증 (Play 모드):
- 경험치 4 → Lv 2, 게임 멈춤, 서로 다른 새 카드 3장, 카드 화면 표시. 고르면 슬롯에 추가되고 다시 진행
- 경험치 2,000을 한 번에 → 29번 연속 선택 (첫 선택 포함 30번 = 6슬롯 × Lv 5), 규칙 위반 0 (최대 레벨 초과·슬롯이 찬 뒤 새 능력·중복 카드·멈추지 않음 모두 0), 카드 1~3장, 이후 레벨업 5번은 카드 없이 500점씩
- 보유한 6종의 효과 값이 레벨 × 수치와 일치 (Quick Swipe 쿨다운 ×0.60, Alternating 0.30, Combo +1.25초, 앞뒤 패턴 +40·3개 패턴 10, 추가 생성 0.50, 옆 자리 0.75)
- 실제 Swipe: Iron Paw ×1.6에서 탭 힘 2.50, 0.8초 뒤 반대 손으로 맞히면 교대 판정 → 3.24 (×1.3), 같은 손 연속·2초 넘게 지남 → 보너스 없음

6단계 수정 (추가 요청: "좌측 상단 글씨 잘리고, 카드나올때 키보드 입력하고 있으면 카드가 나와도 왼쪽으로 쭉 밀린다. 그리고 능력이름은 왜 영어야"):
- 능력 이름·설명, 카드 화면, HUD 문구를 한국어로 (강철 앞발·도미노 효과·재빠른 앞발·넓은 휘두르기·번갈아 치기·콤보 유지·도미노 진열·풀 세트·빽빽한 진열). 카드에는 기획서 영문 이름을 작은 부제로 남김. 내장 폰트에 한글이 없어 Windows 시스템 폰트로 대체 표시됨 → 전용 폰트는 부록 C
- 왼쪽 위 HUD: Game 뷰(904×407)와 1920×1080으로 렌더링해 봤을 때는 잘리지 않았음. 여백을 40 → 48로 늘리고 반투명 바탕을 깔고 글자를 키움
- 카메라: 멈춘 동안(시간 0) `SmoothDamp`에 0이 들어가면 속도가 NaN이 될 수 있어, 시간이 흐를 때만 추적하도록 막음
- 키보드를 누른 채 카드가 나오면 밀리는 문제: 멈춘 동안 고양이 이동 코드는 시간 0이라 움직이지 않아야 하고, 가상 키보드로 재현을 시도했으나 입력이 들어가지 않아 재현 못 함. 무엇이 밀리는지 확인 필요

카드 화면 뒤 화면 밀림 (추가 답변: "화면 전체가 왼쪽으로 쭉 넘어갔었어"):
- 카메라 추적(`SmoothDamp`)은 시간 0에서도 위치·속도가 그대로라 원인이 아님 (직접 계산해 확인). 카메라는 고양이 X만 따라가므로 고양이가 움직인 것
- 가장 유력한 원인: A(←)를 누른 채 카드가 뜨고, 카드를 고르는 순간 시간이 다시 흐르면서 아직 눌린 키로 고양이가 끝까지 이동 → 카메라가 따라가 화면 전체가 밀려 보임
- 수정 (`CatController`): 멈춘 동안에는 이동 처리를 건너뛰고 속도 0. 다시 진행되면 멈추기 전부터 누르고 있던 이동 키·앞발 버튼은 한 번 뗄 때까지 무시
- Unity가 백그라운드에서는 프레임이 멈춰 실제 키 입력으로는 재현·확인하지 못함 → 직접 플레이로 확인 필요

---

## 7단계 — 스테이지와 특수 도자기, 게임 흐름 마무리

> 커밋하고 7단계 가자

스테이지 전환·식탁 열 확장·Fragile/Collector 등장 확률은 4단계에서 데이터로, Collector 점수 2배·경험치 +2는 5단계에서 이미 들어감. 이번에 남은 것과 기획서 완료 기준의 게임 흐름을 마무리.

| 항목 | 내용 |
|---|---|
| Collector's China 머티리얼 | `Vase_Squat` 텍스처에서 무늬(채도 있는 픽셀) → 금색, 유약 → 짙은 남색으로 변환한 `T_Vase_Collector_BaseColor` + 금색 부분만 금속인 `T_Vase_Collector_MetalSmooth`(R 금속도, A 매끄러움) → `M_Vase_Collector` (도자기·파편 모두) |
| Fragile Batch 머티리얼 | `Vase_Tall` 민트 유약의 채도 ×0.55, 밝기 +8% → `M_Vase_Fragile`, 매끄러움 0.85 |
| `GameManager` | Owner ETA 180초 (줄어들기만 함), 0이 되면 게임 종료(시간 정지, 충전 취소), ESC 일시정지 (카드 화면 중에는 무시), 결과 화면에서 R로 다시 시작 |
| `GameOverlayView` | 화면 위 가운데 "집주인 도착까지 m:ss" (마지막 10초는 빨갛게 깜빡임), 스테이지 전환 안내 2초, 일시정지 화면, 결과 화면 (최종 점수·최고 콤보·PURRFECT 횟수·도달 레벨) |
| `TableZoneView` | 아직 열리지 않은 식탁 양 끝 열을 반투명 어두운 덮개로 표시, 스테이지가 오르면 0.6초에 걸쳐 줄어들며 열림 (스테이지 1: 5열, 2: 7열, 3: 전체) |

검증 (Play 모드 직접 진행 180초): 시작 ETA 180 → 61초에 스테이지 2, 121초에 스테이지 3, 181초에 상태 Over·시간 정지·결과 화면. ESC → Paused·시간 0, 다시 ESC → Playing·시간 1. 잠긴 구간 덮개가 스테이지 1에서 양 끝 0.72 m, 스테이지 2에서 0.47 m, 스테이지 3에서 사라짐. 스테이지 2 진입 시 안내 문구 설정 확인.
캡처: `Captures/unity/Vase_Kinds_Normal_Fragile_Collector.png`, `Stage1_LockedZones.png`, `Stage2_Banner.png`, `Result_Screen.png`

왼쪽 위 글씨 잘림 원인 (추가 요청: "글씨 잘리는 문제도 해결해야해"): 게임 화면이 아니라 Unity Game 뷰가 112%로 확대(`m_ZoomArea.m_Scale` 1.12)돼 있어 가장자리가 좌우 약 49 px, 위아래 약 22 px씩 잘려 보였음. Game 뷰 배율을 1배로 되돌림. 플레이 중 마우스 휠을 굴리면 다시 확대될 수 있음 (Game 뷰 위쪽 Scale 슬라이더로 확인).

`UnityConnectSettings.asset`(Unity 서비스 켜짐) 변경은 요청에 따라 되돌림.

손 닿는 거리 (추가 요청: "고양이 팔이 너무 짧아서 아무것도 못건드리는 답답한 상황이 발생해" → 선택: "충격파 판정을 넣고 능력으로 사거리 늘릴 수 있게"):
- 상체 숙이기 검토(Blender 측정): 10° 숙여도 손끝 0.28 → 0.33~0.35 m, 더 숙이면 손이 아래로 내려가 오히려 줄어듦 → 제외
- **발톱 바람(충격파)**: `PawSwipe`가 타격 구간마다 손끝에서 앞발이 향한 방향으로 Sphere Cast를 더 뻗음 (기본 0.18 m). 처음 닿은 도자기 하나에서 막히고, 힘은 거리에 따라 100% → 70%. 판정이 보이도록 `FX_ClawGust` 바람 줄기 입자(늘어난 빌보드)
- **새 능력 "발톱 바람"(Claw Gust)**: 레벨당 사거리 +0.05 m. 능력 10종 (기획서 9종 + 1)
- 검증 (Play 모드, x = +0.20 m에 도자기 하나, 오른손 최대 충전): 맞는 최대 거리 Lv 0 = 0.55 m (기존 0.44 m), Lv 2 = 0.62 m, Lv 3 = 0.69 m(뒷줄), Lv 5 = 0.69 m(힘이 더 셈). 맞은 도자기는 모두 식탁 밖으로 떨어짐. 앞뒤로 줄 선 두 개는 앞의 것에서 바람이 막힘
