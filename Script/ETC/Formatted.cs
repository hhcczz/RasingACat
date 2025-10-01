using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Formatted : MonoBehaviour
{
    public static string FormatKoreanNumber(long num)
    {
        if (num < 1000)
            return num.ToString();

        if (num < 10000)
            return num.ToString("N0"); // 1,234 형식

        string[] units = { "", "만", "억", "조" };
        long[] unitValues = { 1, 10000, 100000000, 1000000000000 };

        // 조부터 거꾸로 검사
        for (int i = units.Length - 1; i >= 0; i--)
        {
            if (num >= unitValues[i])
            {
                long major = num / unitValues[i];          // 주 단위 값
                long remainder = num % unitValues[i];      // 나머지

                // 억 단위 이상일 때는 간략화
                if (i >= 2) // 억, 조
                {
                    // 천만, 천억 단위까지만 표시
                    long sub = remainder / (unitValues[i - 1] * 1000); // "천만" or "천억"
                    if (sub > 0)
                        return $"{major}{units[i]} {sub}천{units[i - 1]}";
                    else
                        return $"{major}{units[i]}";
                }
                else
                {
                    // 만 단위 이하는 세부 표현
                    if (remainder == 0)
                        return $"{major}{units[i]}";
                    if (remainder < 1000)
                        return $"{major}{units[i]} {remainder}";
                    else
                        return $"{major}{units[i]} {remainder.ToString("N0")}";
                }
            }
        }

        return num.ToString("N0");
    }

    public static string FormatTimeDouble(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes}:{seconds:00}";
    }
}
