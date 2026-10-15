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
