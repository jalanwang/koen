# KoEnCaret

텍스트 입력 캐럿(커서) 옆에 현재 **한/영 입력 상태**를 국기로 보여주는 Windows 유틸리티입니다.

| 상태 | 표시 |
|------|------|
| 한글 입력 | 태극기 |
| 영문 입력 | 성조기 |

- 캐럿 오른쪽 아래에 작은 국기가 따라다닙니다 (클릭 통과, 포커스를 가져가지 않음).
- 트레이 아이콘에도 현재 상태가 표시되며, 우클릭 → "종료"로 끌 수 있습니다.
- 한 번 설치하면 로그온할 때마다 자동으로 실행됩니다.

## 요구 사항

- Windows 10 / 11
- .NET Framework 4 (Windows 기본 포함, 별도 설치 불필요)

## 빌드

```cmd
build.cmd
```

Windows 내장 C# 컴파일러(`csc.exe`)로 `KoEnCaret.exe`를 만듭니다. 별도 SDK는 필요 없습니다.

## 설치 / 제거

PowerShell에서 실행합니다. 관리자 권한은 필요하지 않습니다.

```powershell
# 설치 (exe가 없으면 먼저 빌드)
powershell -ExecutionPolicy Bypass -File install.ps1

# 제거
powershell -ExecutionPolicy Bypass -File uninstall.ps1
```

설치 시 다음이 수행됩니다.

1. `KoEnCaret.exe`를 `%LOCALAPPDATA%\KoEnCaret\`에 복사
2. `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`에 `KoEnCaret` 등록 (로그온 시 자동 실행)
3. 즉시 실행

제거 시 프로세스 종료, 자동 실행 등록 삭제, 설치 폴더 삭제를 수행합니다.

## 동작 방식

100ms마다 다음을 확인합니다.

1. **캐럿 위치**: `GetGUIThreadInfo`(클래식 Win32) → 실패 시 MSAA `OBJID_CARET`(Chrome, Edge, Office 등)
2. **한/영 상태**: 키보드 레이아웃이 한국어일 때 IME의 변환 모드(`IME_CMODE_NATIVE`)를 조회

캐럿 위치를 얻지 못하면 국기를 숨깁니다.

## 알려진 한계

- 캐럿 정보를 제공하지 않는 앱(일부 Electron 앱, 게임 등)에서는 국기가 표시되지 않을 수 있습니다.
- 관리자 권한으로 실행된 앱에서는 일반 권한 프로세스가 캐럿 정보에 접근할 수 없어 표시되지 않습니다.
- Windows 11의 새 입력기를 쓰는 일부 앱에서는 한/영 상태가 잘못 표시될 수 있습니다.

## 파일 구성

| 파일 | 설명 |
|------|------|
| `KoEnCaret.cs` | 프로그램 본체 |
| `build.cmd` | 빌드 스크립트 |
| `install.ps1` | 설치 스크립트 |
| `uninstall.ps1` | 제거 스크립트 |

## 라이선스

[LICENSE](LICENSE)를 참고하세요.
