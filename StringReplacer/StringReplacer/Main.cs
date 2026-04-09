using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StringReplacer
{
    // JSON 변환을 위한 데이터 구조체
    [Serializable]
    public class Data
    {
        public string Original;
        public string Replacement;
    }

    // 유니티 JsonUtility는 리스트를 바로 인식하지 못하므로 Wrapper가 필요합니다.
    [Serializable]
    public class DataWrapper
    {
        public List<Data> Items;
    }

    [BepInPlugin("String.Replacer", "String Replacer", "1.0.0")]
    public class Main : BasePlugin
    {
        public Dictionary<string, string> Loaded = new Dictionary<string, string>();
        public static Main Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
            Log.LogWarning("Reading Text Databases");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "TextDatabases");
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.json");
                foreach (var file in files)
                {
                    Log.LogInfo($"FOUND:{file}");
                    LoadTextFiles(file);
                }
                Log.LogWarning($"Finished Reading Text Databases...Found:{files.Length}");
            }
            else
            {
                Log.LogWarning("TextDatabases folder not found, creating one");
                Directory.CreateDirectory(path);
            }
            AddComponent<GUIComponent>();
        }

        public class GUIComponent : MonoBehaviour
        {
            private void Update()
            {
                if (Main.Instance == null) return;

                if (Input.GetKeyDown(KeyCode.N))
                {
                    var allTexts = Main.Instance.GetAllTexts();
                    if (allTexts != null) Main.Instance.GetText(allTexts);
                }

                var allTextsInScene = UnityEngine.Object.FindObjectsOfType<Text>();
                foreach (var text in allTextsInScene)
                {
                    if (text != null) Main.Instance.ReplaceString(text);
                }
            }
        }

        bool ReplaceString(Text target)
        {
            if (Loaded.TryGetValue(target.text, out string replacement))
            {
                target.text = replacement;
                return true;
            }
            return false;
        }

        void LoadTextFiles(string file)
        {
            try
            {
                string json = File.ReadAllText(file);
                // JsonUtility를 사용하여 리스트 복구
                var wrapper = JsonUtility.FromJson<DataWrapper>("{\"Items\":" + json + "}");

                if (wrapper != null && wrapper.Items != null)
                {
                    foreach (var data in wrapper.Items)
                    {
                        if (data.Original != data.Replacement)
                        {
                            if (!Loaded.ContainsKey(data.Original))
                                Loaded.Add(data.Original, data.Replacement);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading file {file}: {ex.Message}");
            }
        }

        List<Text> GetAllTexts() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Text>(true)).ToList();

        public void GetText(List<Text> texts)
        {
            // 1. 저장할 경로 설정 (TextDatabases 폴더)
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "TextDatabases");

            // 폴더가 없으면 생성
            if (!Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }

            Log.LogInfo($"Scanning {texts.Count} text components...");

            var dataList = new List<Data>();
            foreach (var text in texts)
            {
                if (text == null || string.IsNullOrEmpty(text.text)) continue;

                if (!dataList.Any(d => d.Original == text.text))
                {
                    dataList.Add(new Data { Original = text.text, Replacement = text.text });
                }
            }

            Log.LogInfo($"Unique strings to save: {dataList.Count}");

            if (dataList.Count == 0)
            {
                Log.LogWarning("No strings found in this scene. Skip saving.");
                return;
            }

            // 2. 수동 JSON 문자열 생성
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < dataList.Count; i++)
            {
                // 텍스트 정제: 실제 줄바꿈을 문자인 "\n"으로 변환해야 JSON이 안 깨짐
                string escapedOriginal = dataList[i].Original
                    .Replace("\\", "\\\\")  // 역슬래시 먼저 처리
                    .Replace("\"", "\\\"")  // 큰따옴표 처리
                    .Replace("\r", "")      // 윈도우식 줄바꿈 제거
                    .Replace("\n", "\\n");  // 실제 줄바꿈을 "\n"이라는 텍스트로 치환

                string escapedReplacement = dataList[i].Replacement
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "")
                    .Replace("\n", "\\n");

                sb.AppendLine("  {");
                sb.AppendLine($"    \"Original\": \"{escapedOriginal}\",");
                sb.AppendLine($"    \"Replacement\": \"{escapedReplacement}\"");
                sb.Append("  }" + (i < dataList.Count - 1 ? "," : ""));
                sb.AppendLine();
            }
            sb.AppendLine("]");

            string fullPath = Path.Combine(dbPath, $"{SceneManager.GetActiveScene().name}.json");

            try
            {
                // 핵심: Encoding.UTF8을 명시하여 중국어 깨짐 방지
                File.WriteAllText(fullPath, sb.ToString(), System.Text.Encoding.UTF8);
                Log.LogMessage($"[SUCCESS] Saved UTF-8 JSON: {fullPath}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Save Failed: {ex.Message}");
            }
        }

        private static string format_json(string json)
        {
            // 이미 GetText에서 수동으로 예쁘게 만들었으므로 그냥 반환합니다.
            return json;
        }
    }

    public static class Extensions
    {
        public static List<T> GetAllComponentsInArray<T>(this GameObject[] source)
        {
            List<T> a = new List<T>();

            source.ToList().ForEach(gobject =>
            {
                if (gobject.TryGetComponent<T>(out var s))
                {
                    a.Add(gobject.GetComponent<T>());
                }
                a.AddRange(gobject.ListChildren().ToArray().GetAllComponentsInArray<T>());
            });
            return a;
        }

        public static List<GameObject> ListChildren(this GameObject parent)
        {
            List<GameObject> a = new List<GameObject>();
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                a.Add(parent.transform.GetChild(i).gameObject);
            }
            return a;
        }
    }
}
