using UnityEngine;

namespace FinanceQuest
{
    // Napędza animację wciśnięcia karty. Włączony tylko podczas ruchu,
    // więc karty w spoczynku nie kosztują nic w każdej klatce.
    public class LipSpring : MonoBehaviour
    {
        public LipCard Card;

        void Update()
        {
            if (Card == null || Card.Step(Time.unscaledDeltaTime)) enabled = false;
        }
    }
}
