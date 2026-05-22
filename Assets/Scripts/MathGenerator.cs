using UnityEngine;

namespace MathsClass
{
    // Génère un calcul dont le résultat est forcément un entier 0–9.
    // 7 paliers de difficulté (cf. GAME_DESIGN.md).
    public static class MathGenerator
    {
        public struct Question
        {
            public string display;
            public int answer;
        }

        public enum Tier { Easy, Sub, Mul, Mixed, Div, Chaos, Hell }

        public static Question Generate(Tier tier)
        {
            for (int i = 0; i < 200; i++)
            {
                var q = TryGenerate(tier);
                if (q.answer >= 0 && q.answer <= 9 && !string.IsNullOrEmpty(q.display)) return q;
            }
            return new Question { display = "1 + 1", answer = 2 };
        }

        static Question TryGenerate(Tier tier)
        {
            switch (tier)
            {
                case Tier.Easy:
                {
                    int a = Random.Range(0, 10);
                    int b = Random.Range(0, 10);
                    if (a + b > 9) return new Question { display = "", answer = -1 };
                    return new Question { display = $"{a} + {b}", answer = a + b };
                }
                case Tier.Sub:
                {
                    if (Random.value < 0.5f) goto case Tier.Easy;
                    int a = Random.Range(0, 10);
                    int b = Random.Range(0, 10);
                    if (a - b < 0) return new Question { display = "", answer = -1 };
                    return new Question { display = $"{a} − {b}", answer = a - b };
                }
                case Tier.Mul:
                {
                    int a = Random.Range(0, 5);
                    int b = Random.Range(0, 5);
                    return new Question { display = $"{a} × {b}", answer = a * b };
                }
                case Tier.Mixed:
                {
                    int a = Random.Range(0, 5);
                    int b = Random.Range(0, 5);
                    int c = Random.Range(0, 5);
                    int res = a + b * c;
                    if (res > 9) return new Question { display = "", answer = -1 };
                    return new Question { display = $"{a} + {b} × {c}", answer = res };
                }
                case Tier.Div:
                {
                    int b = Random.Range(1, 6);
                    int answer = Random.Range(0, 10);
                    int a = b * answer;
                    return new Question { display = $"{a} ÷ {b}", answer = answer };
                }
                case Tier.Chaos:
                {
                    int a = Random.Range(1, 6);
                    int b = Random.Range(0, 6);
                    int c = Random.Range(1, 4);
                    int sum = a + b;
                    if (sum % c != 0) return new Question { display = "", answer = -1 };
                    return new Question { display = $"({a} + {b}) ÷ {c}", answer = sum / c };
                }
                case Tier.Hell:
                {
                    int a = Random.Range(2, 10);
                    int b = Random.Range(2, 10);
                    int prod = a * b;
                    int target = Random.Range(0, 10);
                    int c = prod - target;
                    if (c < 0) return new Question { display = "", answer = -1 };
                    return new Question { display = $"{a} × {b} − {c}", answer = target };
                }
            }
            return new Question { display = "1 + 1", answer = 2 };
        }
    }
}
