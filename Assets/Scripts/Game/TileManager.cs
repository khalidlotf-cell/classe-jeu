using UnityEngine;

namespace MathsClass
{
    // Centralise les 10 dalles 0-9. Permet au GameManager de récupérer une dalle par numéro,
    // de toutes les reset, et d'appliquer le mode daltonien (formes différenciées).
    public class TileManager : MonoBehaviour
    {
        public Tile[] tiles = new Tile[10]; // index = numéro

        void Awake()
        {
            // Auto-fill si l'inspecteur n'a pas été assigné
            if (tiles == null || tiles.Length != 10)
            {
                var found = FindObjectsByType<Tile>(FindObjectsSortMode.None);
                tiles = new Tile[10];
                foreach (var t in found)
                {
                    if (t.number >= 0 && t.number < 10) tiles[t.number] = t;
                }
            }
        }

        public void ResetAll()
        {
            foreach (var t in tiles) if (t) t.ResetTrigger();
        }

        public Vector3 GetPosition(int number)
        {
            if (number < 0 || number >= 10 || tiles[number] == null) return Vector3.zero;
            return tiles[number].transform.position;
        }

        public void ApplyColorblind(bool enabled)
        {
            foreach (var t in tiles) if (t) t.SetColorblind(enabled);
        }
    }
}
