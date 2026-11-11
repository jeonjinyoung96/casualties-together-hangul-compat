# Casualties Together 한글 발화 호환 플러그인

Casualties Unknown에서 Together 멀티와 XTMP 한글패치를 함께 사용할 때 캐릭터 머리 위 발화의 글자가 깨지는 문제를 수정합니다. 멀티모드가 만든 발화 UI가 XTMP 적용 후 실제로 사용하는 폰트의 재질을 쓰도록 합니다.

## 설치

1. Casualties Unknown을 종료합니다.
2. [릴리스](https://github.com/jeonjinyoung96/casualties-together-hangul-compat/releases/latest)에서 `CasualtiesTogetherHangulCompat-v1.0.0.zip`을 내려받습니다.
3. 게임 실행 파일이 있는 폴더에 압축을 풀고 기존 `BepInEx` 폴더와 병합합니다.
4. 멀티 게임에서 한글 발화를 확인합니다. 이 패치는 발화의 로컬 화면 표시에 적용되므로, 호스트와 참가자 각자의 화면을 고치려면 해당 PC에 설치합니다.

설치 후 DLL 경로는 `BepInEx/plugins/CasualtiesTogetherHangulCompat/CasualtiesTogetherHangulCompat.dll`입니다. 제거하려면 이 DLL을 삭제합니다.

## 지원 환경

- Casualties Unknown 1.0
- BepInEx 5.4.23.5
- Casualties Together 5.5.0
- XTMP 1.0.5가 포함된 `cu-1.0-Hangeul-xtmpi-261110` 한글패치

패치를 적용하면 BepInEx 로그에 `Together overhead speech now uses the displayed font's material.`이 남습니다. 게임이나 멀티모드 업데이트로 대상 메서드가 바뀌면 로그에 경고가 남습니다.

위 조합으로 게임을 시작해 플러그인 로드와 발화 메서드 패치 적용을 확인했습니다. 멀티 세션에서 실제 한글 발화가 그려지는 화면은 확인이 필요합니다.

## 빌드

Windows PowerShell에서 실행합니다. 기본 경로가 아닌 곳에 게임을 설치했다면 `-GamePath`를 지정합니다.

```powershell
.\Build.ps1
.\Build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Casualties Unknown'
```

빌드 결과는 `dist`에 생성됩니다. 설치용 ZIP에는 이 호환 플러그인 DLL 한 개가 들어 있습니다.
