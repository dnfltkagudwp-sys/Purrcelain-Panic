using System;
using UnityEngine;

namespace PurrcelainPanic
{
    public enum SpawnPattern { Single, Pair, Line2, Line3, Cluster }

    /// <summary>스테이지 하나의 생성 규칙 (기획서 부록 B-8). 확률은 가중치라 합이 100이 아니어도 된다.</summary>
    [Serializable]
    public class StageSettings
    {
        public float startTime;         // 게임 시작부터, 초
        public float interval = 2.5f;   // 생성 간격, 초
        public int columns = 5;         // 가운데부터 쓰는 열 수 (홀수)

        [Header("패턴 가중치")]
        public float single;
        public float pair;
        public float line2;
        public float line3;
        public float cluster;

        [Header("도자기 종류 가중치")]
        public float normal = 100f;
        public float fragile;
        public float collector;

        public float PatternWeight(SpawnPattern p) => p switch
        {
            SpawnPattern.Single => single,
            SpawnPattern.Pair => pair,
            SpawnPattern.Line2 => line2,
            SpawnPattern.Line3 => line3,
            _ => cluster,
        };

        public float KindWeight(VaseKind k) => k switch
        {
            VaseKind.Normal => normal,
            VaseKind.Fragile => fragile,
            _ => collector,
        };

        /// <summary>가중치 배열에서 하나를 고른다.</summary>
        public static int Pick(float[] weights)
        {
            float total = 0f;
            foreach (var w in weights)
                total += Mathf.Max(0f, w);
            if (total <= 0f)
                return 0;
            float r = UnityEngine.Random.value * total;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= Mathf.Max(0f, weights[i]);
                if (r < 0f)
                    return i;
            }
            return weights.Length - 1;
        }
    }
}
