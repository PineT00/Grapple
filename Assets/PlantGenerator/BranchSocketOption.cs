using System;
using UnityEngine;

namespace PlantGenerator
{
    /// <summary>
    /// 소켓에 연결 가능한 브랜치 옵션 (확률 포함)
    /// </summary>
    [Serializable]
    public class BranchSocketOption
    {
        [SerializeField] BranchTemplate template;
        [SerializeField] [Range(0, 100)] float probabilityPercent = 100f;

        public BranchTemplate Template
        {
            get => template;
            set => template = value;
        }

        public float ProbabilityPercent
        {
            get => probabilityPercent;
            set => probabilityPercent = value;
        }
    }
}
