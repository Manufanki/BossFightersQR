using System.Collections.Generic;
using UnityEngine;

// A scanned QR card's data, used as the action it performs when played in the Action Phase.
[CreateAssetMenu(fileName = "NewCard", menuName = "BossFightersQR/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    [Tooltip("The exact string encoded in the card's QR code.")]
    public string qrId;
    public HeroType heroType;
    public ClassType classType;
    [Tooltip("Effects resolve from top to bottom when this card is played.")]
    [SerializeReference] public List<CardEffect> effects = new List<CardEffect>();

}
