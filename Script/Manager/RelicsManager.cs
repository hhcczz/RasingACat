using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicsManager : MonoBehaviour
{
    public static RelicsManager Instance;

    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } }

    public enum Type { Merge, Egg }

    public int Player_RelicsLv = 0;
    public int maxLevel = 27;
    public struct NeedCost
    {
        public int catcoin;
        public int catGoldcoin;
        public NeedCost(int c, int e) { catcoin = c; catGoldcoin = e; }
    }

    public NeedCost[] _needCost;
    public int[] SuccessProbability;
    public int[] BreakProbability;

    public float[] JackpotMergeProbability;
    public float[] JackpotCatEggProbability;

    void Start()
    {
        _needCost = new NeedCost[27];

        for(int i = 0; i < _needCost.Length; i++)
        {
            int index = i;

            if (index < 10) _needCost[index] = new NeedCost(index * 10 + 10, 0);
            else _needCost[index] = new NeedCost(index * 30, index);
        }

        // Real
        SuccessProbability = new int[27]
        {
            95, 90, 85, 80, 75, 70, 65, 60, 55, 50,
            45, 40, 35, 30, 25, 20, 15, 15, 15, 15,
            10, 10, 9, 8, 7, 6, 5,
        };

        //Debug
        //{
        //    95, 90, 85, 80, 75, 70, 65, 60, 55, 50,
        //    45, 100, 100, 100, 100, 100, 100, 100, 100,
        //    100, 100, 100, 100, 100, 100, 100, 100,
        //};

        BreakProbability = new int[27]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            15, 20, 25, 30, 35, 40, 45,
        };

        JackpotMergeProbability = new float[27]
        {
            0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.08f, 0.10f, 0.12f, 0.14f,
            0.17f, 0.20f, 0.23f, 0.26f, 0.30f, 0.34f, 0.38f, 0.5f, 0.8f, 1.1f,
            2.2f, 4.4f, 8.8f, 17.6f, 22.2f, 30.0f, 50.0f,
        };

        JackpotCatEggProbability = new float[27]
        {
            0.01f, 0.02f, 0.03f, 0.04f, 0.05f,
            0.06f, 0.07f, 0.08f, 0.09f, 0.10f,
            0.12f, 0.14f, 0.16f, 0.18f, 0.20f, 0.25f,
            0.30f, 0.35f, 0.40f, 0.45f, 0.50f, 0.55f,
            1.00f, 2.00f, 4.00f, 8.00f, 10.00f,
        };
    }

    public float GetAbilityValue(Type type)
    {
        if (type == Type.Merge) return JackpotMergeProbability[Player_RelicsLv];
        else return JackpotCatEggProbability[Player_RelicsLv];
    }

    public int GetRelicsLevel(int bonus = 0)
    {
        return Player_RelicsLv >= 0 ? Player_RelicsLv + bonus : 0;
    }

    public void AddRelicsLevel(int amount = 1)
    {
        Player_RelicsLv = System.Math.Min(Player_RelicsLv + amount, 27);
    }

    public NeedCost GetNeedCost()
    {
        if (_needCost == null || Player_RelicsLv < 0 || Player_RelicsLv >= _needCost.Length)
        {
            return new NeedCost(0, 0); // ±âº»°ª
        }
        return _needCost[Player_RelicsLv];
    }
}
