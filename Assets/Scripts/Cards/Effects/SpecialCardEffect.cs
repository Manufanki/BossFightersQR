using System;
using UnityEngine;

// Star-symbol effect: free-text special rule described on the card.
[Serializable]
public class SpecialCardEffect : CardEffect
{
    [TextArea] public string description;
}
