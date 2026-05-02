# 문자열 대체기(string Replacer)
 json 파일 기반 문자열 대체 BepInEx 플러그인

 ### 사용법

 N을 눌러 Scene의 모든 문자열을 하나의 json 파일에 기록합니다

 생성된 파일을 수정한 뒤 게임 파일 안의 TextDatabases에 넣으십시오

 ### 주요 변경 및 개선 사항

 1. **TMP(TextMeshPro) 대응**: `TMP_Text` 컴포넌트를 함께 검색합니다. 탭 키로 열리는 최신 UI들은 대부분 [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)를 사용하기 때문에 필수적인 수정입니다.
 2. **FindObjectsOfTypeAll**: `SceneManager`가 잡지 못하는 `DontDestroyOnLoad` Scene의 Object(주로 모드 UI나 시스템 UI)를 잡기 위해 사용합니다.
 3. **Dictionary 최적화**: `ContainsKey` 대신 `TryGetValue`를 사용하여 텍스트 교체 속도를 높였습니다.
 4. 수정된 코드는 Legacy Text와 TextMeshPro를 모두 지원하며, Resources.FindObjectsOfTypeAll을 사용하여 Scene 전체에 숨겨진 UI까지 탐색합니다.
