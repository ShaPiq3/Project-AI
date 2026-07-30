using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class VNCSVParser
{
    private static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
    private static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private static char[] TRIM_CHARS = { '\"', ' ' };

    public static List<DialogueRow> ParseCSV(string fileName)
    {
        List<DialogueRow> list = new List<DialogueRow>();
        TextAsset data = Resources.Load<TextAsset>(fileName);

        if (data == null)
        {
            Debug.LogError($"CSV file not found at Resources/{fileName}");
            return list;
        }

        string[] lines = Regex.Split(data.text, LINE_SPLIT_RE);
        if (lines.Length <= 1) return list;

        string[] rawHeaders = Regex.Split(lines[0], SPLIT_RE);
        List<string> cleanHeaders = new List<string>();
        foreach (var h in rawHeaders)
        {
            if (h != null) cleanHeaders.Add(h.Trim(TRIM_CHARS).ToLower());
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]) || string.IsNullOrEmpty(lines[i].Trim())) continue;

            string[] fields = Regex.Split(lines[i], SPLIT_RE);
            if (fields == null || fields.Length == 0 || string.IsNullOrEmpty(fields[0].Trim())) continue;

            DialogueRow row = new DialogueRow();

            row.id = GetField(fields, cleanHeaders, "id");
            row.speaker = GetField(fields, cleanHeaders, "speaker");
            row.dialogue = GetField(fields, cleanHeaders, "dialogue");
            row.background = GetField(fields, cleanHeaders, "background");
            row.standingLeft = GetField(fields, cleanHeaders, "standing_left");
            row.standingMid = GetField(fields, cleanHeaders, "standing_mid");
            row.standingRight = GetField(fields, cleanHeaders, "standing_right");
            row.sfx = GetField(fields, cleanHeaders, "sfx");
            row.bgm = GetField(fields, cleanHeaders, "bgm");
            row.ambience = GetField(fields, cleanHeaders, "ambience");
            row.effect = GetField(fields, cleanHeaders, "effect");

            row.choice1 = GetField(fields, cleanHeaders, "choice_1");
            row.nextId1 = GetField(fields, cleanHeaders, "next_id_1");
            row.choice2 = GetField(fields, cleanHeaders, "choice_2");
            row.nextId2 = GetField(fields, cleanHeaders, "next_id_2");
            row.choice3 = GetField(fields, cleanHeaders, "choice_3");
            row.nextId3 = GetField(fields, cleanHeaders, "next_id_3");

            row.nextId = GetField(fields, cleanHeaders, "next_id");

            // ID가 명시되어 있다면 dialogue가 비어있어도 파싱 리스트에 포함시킵니다.
            if (row != null && !string.IsNullOrEmpty(row.id))
            {
                list.Add(row);
            }
        }

        return list;
    }

    private static string GetField(string[] fields, List<string> headers, string columnName)
    {
        if (headers == null || fields == null) return string.Empty;
        int index = headers.FindIndex(h => h.Equals(columnName.ToLower()));
        if (index >= 0 && index < fields.Length)
        {
            if (fields[index] == null) return string.Empty;
            return fields[index].Trim(TRIM_CHARS).Replace("\\n", "\n");
        }
        return string.Empty;
    }
}