using System;
using System.Collections.Generic;
using UnityEngine;

// Builds a fast runtime lookup from scanned QR IDs to their CardData definitions.
public class CardRegistry
{
    private readonly Dictionary<string, CardData> _cardsByQrId = new Dictionary<string, CardData>(StringComparer.Ordinal);

    public int Count => _cardsByQrId.Count;

    public CardRegistry(CardDatabase database)
    {
        if (database == null)
        {
            Debug.LogWarning("[CardRegistry] No CardDatabase assigned; no cards are available.");
            return;
        }

        foreach (CardData card in database.Cards)
        {
            if (card == null)
            {
                Debug.LogWarning("[CardRegistry] CardDatabase contains an empty card entry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(card.qrId))
            {
                Debug.LogWarning($"[CardRegistry] Card '{card.cardName}' has no QR ID and cannot be scanned.");
                continue;
            }

            if (!_cardsByQrId.TryAdd(card.qrId, card))
                Debug.LogError($"[CardRegistry] Duplicate QR ID '{card.qrId}' on card '{card.cardName}'. The first card remains registered.");
        }
    }

    public bool TryGetCard(string qrId, out CardData card)
    {
        card = null;
        return !string.IsNullOrWhiteSpace(qrId) && _cardsByQrId.TryGetValue(qrId, out card);
    }
}