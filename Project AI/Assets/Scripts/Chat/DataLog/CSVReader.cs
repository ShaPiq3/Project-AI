using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CSVReader
{
    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    public static List<Dictionary<string, object>> Read(string file)
    {
        var list = new List<Dictionary<string, object>>();
        TextAsset data = Resources.Load(file) as TextAsset;

        if (data == null)
        {
            Debug.LogError($"CSV File not found in Resources: {file}");
            return list;
        }

        var lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length <= 1) return list;

        // 🌟 [수정] 헤더 배열을 파싱한 후, 각 헤더 이름의 공백과 BOM 문자를 제거합니다.
        var rawHeader = Regex.Split(lines[0], SPLIT_RE);
        var header = new string[rawHeader.Length];
        for (int h = 0; h < rawHeader.Length; h++)
        {
            // \uFEFF(BOM 문자)를 제거하고 앞뒤 공백을 잘라냅니다.
            header[h] = rawHeader[h].Replace("\uFEFF", "").Trim();
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                // 헤더 자체가 비어있다면 데이터 매칭 스킵
                if (string.IsNullOrEmpty(header[j])) continue;

                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                object finalvalue = value;
                int n;
                float f;
                if (int.TryParse(value, out n))
                {
                    finalvalue = n;
                }
                else if (float.TryParse(value, out f))
                {
                    finalvalue = f;
                }
                entry[header[j]] = finalvalue;
            }
            list.Add(entry);
        }
        return list;
    }
}