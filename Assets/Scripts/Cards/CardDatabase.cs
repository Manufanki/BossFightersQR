using System.Collections.Generic;
using UnityEngine;

// The Inspector-authored catalog of every CardData asset included in this build.
[CreateAssetMenu(fileName = "CardDatabase", menuName = "BossFightersQR/Card Database")]
public class CardDatabase : ScriptableObject
{
    [SerializeField] private List<CardData> cards = new List<CardData>();

    public IReadOnlyList<CardData> Cards => cards;
}