using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PopupChatData", menuName = "Scriptable Object/PopupChatData", order = 1)]
public class PopupChatData : ScriptableObject
{
    [Serializable]
    public class Entity
    {
        public int id;
        public string context;
        public string sender;
        public string message;
        public string linkPanel;
    }

    public List<Entity> Entities;
}